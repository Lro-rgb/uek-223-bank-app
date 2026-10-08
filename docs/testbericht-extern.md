# Testbericht Bank-App

> **Externer Test · ÜK 223**
> Unabhängiger Test der Bank-App von Matti & Luis durch eine andere Gruppe: eure Integrationstests und den Lasttest nachgefahren, danach eigene API-, Nebenläufigkeits- und Oberflächentests.

| Datum | Version | Umgebung | Datenbank |
|---|---|---|---|
| 8. Oktober 2026 | `main` · `0f7a6ed` | MariaDB 11.6.2, Backend :5000, Frontend :4200, Chromium | frisch aufgesetzt nach README |

## Inhalt

- [Zusammenfassung](#zusammenfassung)
- [Befunde im Detail](#befunde-im-detail)
- [Geprüft und in Ordnung](#geprüft-und-in-ordnung)
- [Von euch bereits dokumentiert — bestätigt](#von-euch-bereits-dokumentiert--bestätigt)
- [F1 nachstellen](#f1-nachstellen)
- [Priorität aus unserer Sicht](#priorität-aus-unserer-sicht)

---

## Zusammenfassung

| | |
|---|---|
| ✅ **20/20** | Integrationstests grün |
| ✅ **0** | 5xx bei 1500 Anfragen im Lasttest |
| **8** | Befunde ausserhalb der Transaktionslogik |
| 🔴 **1** | davon kritisch |

**Die Transaktionssicherheit haltet ihr ein.** Alle eure Nachweise liessen sich reproduzieren: 20 von 20 Tests grün, der NBomber-Lasttest PASS, und in eigenen Nebenläufigkeitstests ging kein Geld verloren, kein Saldo wurde negativ und es gab keinen einzigen 5xx. Besonders sauber ist das Löschen gegen eine gleichzeitige Buchung.

Gefunden haben wir acht Fehler *ausserhalb* der Transaktionslogik, davon einen kritischen: Ein Konto ohne Namen legt die ganze Kontenliste dauerhaft lahm. Die übrigen betreffen den API-Vertrag und die Oberfläche.

| Nr. | Befund | Schwere |
|---|---|---|
| [F1](#f1--konto-ohne-namen-legt-die-kontenliste-lahm) | `GET /Ledgers` hängt für immer, sobald ein Konto `name = NULL` hat — Endlosschleife, Neustart nötig | 🔴 kritisch |
| [F2](#f2--put-ledgersid-ignoriert-die-id-aus-der-url) | `PUT /Ledgers/{id}` ignoriert die ID aus der URL und nimmt die aus dem Body | 🟠 mittel |
| [F3](#f3--doppelklick-auf-buchen-bucht-zweimal) | Doppelklick auf „Buchen“ bucht zweimal | 🟠 mittel |
| [F4](#f4--ledgers-ist-ohne-login-erreichbar) | `/ledgers` ist ohne Login erreichbar — `AuthGuard` importiert, aber nicht eingetragen | 🟠 mittel |
| [F5](#f5--unbekanntes-konto-liefert-204-statt-404) | `GET /Ledgers/{id}` liefert bei unbekannter ID `204` statt `404` | ⚪ klein |
| [F6](#f6--ungültiges-json-bricht-euren-api-vertrag) | Ungültiges JSON bricht euren API-Vertrag `{ "message": … }` | ⚪ klein |
| [F7](#f7--beträge-unter-einem-rappen-werden-angenommen) | Beträge unter einem Rappen werden angenommen | ⚪ klein |
| [F8](#f8--lint-ist-rot-die-ci-prüft-es-nicht) | `ng lint` ist rot, die CI prüft nur mdBook | ⚪ klein |

---

## Befunde im Detail

### F1 — Konto ohne Namen legt die Kontenliste lahm

**Schwere:** 🔴 kritisch

**Sobald irgendein Konto `name = NULL` hat, antwortet `GET /api/v1/Ledgers` nie mehr. Kein Timeout, kein Fehler, keine Antwort.**

Dasselbe gilt für `GET /api/v1/Ledgers/{id}`. Damit ist die Anwendung für *alle* Benutzer:innen unbrauchbar, auch für `testuser` — die Kontenliste ist die Startseite nach dem Login. Nur ein Neustart von `Bank.Web` hilft.

#### Ursache

`LedgerRepository.GetAllLedgers()` und gleich daneben `SelectOne(int)` lesen den Namen mit `reader.GetString(reader.GetOrdinal("name"))`. Bei `NULL` wirft das, und der `catch` darunter macht daraus eine Endlosschleife:

```csharp
catch (Exception ex)
{
    try
    {
        transaction.Rollback();
        if (ex.GetType() != typeof(Exception))
            worked = false;        // bei JEDER echten Exception
    }
    ...
} while (!worked);               // startet neu — mit unveränderten Daten
```

Die geworfene Exception ist keine blanke `Exception`, also wird `worked = false` und die `do…while`-Schleife beginnt von vorn. Sie wirft sofort wieder. Das läuft endlos.

#### Messung

Jede Anfrage hinterlässt einen Thread, der dauerhaft rechnet. Die Last addiert sich:

| Gleichzeitig hängende Anfragen | CPU-Ticks in 5 s |
|---:|---:|
| 1 | 149 |
| 4 | 422 |

Schon ein paar Seitenaufrufe lasten damit einen Kern komplett aus. Die Threads verschwinden erst beim Neustart.

#### Wie man es auslöst

Nicht nur über `"name": null`. Gefährlicher ist der normale Fall, dass jemand nur den Saldo ändern will und einen **unvollständigen Body** schickt:

```http
PUT /api/v1/Ledgers/4   {"id":4,"balance":50}   -> 200 OK
```

`name` fehlt im JSON, wird damit zu `null`, und `Update()` schreibt `name=NULL` in die DB. Danach ist die App tot. Beide Wege getestet und reproduziert.

#### Ihr habt die Lösung schon

`GET /api/v1/Bookings?ledgerId=4` funktioniert bei genau demselben Konto einwandfrei (`200`, leere Liste). Dort lest ihr den Namen mit `ReadNullableString()`, und IT-H5 prüft das sogar. Die Lösung existiert also bereits im `BookingRepository` — sie fehlt nur im `LedgerRepository`.

> **Vorschlag.** In `GetAllLedgers()` und `SelectOne()` denselben `ReadNullableString()`-Ansatz verwenden. Unabhängig davon sollte die Retry-Schleife nur bei *transienten* Fehlern (1213/1205) neu starten, so wie ihr es im `BookingRepository` mit `IsTransient()` schon richtig macht — sonst wird aus jedem unerwarteten Fehler eine Endlosschleife. Zusätzlich `name` in der DB auf `NOT NULL` setzen.

---

### F2 — `PUT /Ledgers/{id}` ignoriert die ID aus der URL

**Schwere:** 🟠 mittel

`LedgersController.Put(int id, [FromBody] Ledger ledger)` verwendet den Parameter `id` nicht, sondern ruft direkt `ledgerRepository.Update(ledger)` auf. Entscheidend ist also allein die ID im Body.

```http
PUT /api/v1/Ledgers/3
Body: {"id":2,"name":"UMBENANNT-VIA-ID-2","balance":3943.0000001}

-> 200 OK — geändert wurde Konto 2, nicht Konto 3
```

Aus demselben Code folgen zwei weitere Dinge: Ein unvollständiger Body setzt `name` auf `NULL` (siehe F1), und `balance` lässt sich beliebig überschreiben, womit Geld aus dem Nichts entsteht (im Test `0` → `50`). Dass Administrators den Saldo ändern dürfen, steht so in eurer Anforderung A3 — in Kombination mit dem unvollständigen Body passiert es aber auch unabsichtlich. Ausserdem meldet ein `PUT` auf eine nicht existierende ID `200`, obwohl nichts geändert wurde.

Der Code stammt aus der Kursvorlage, ist bei euch aber aktiv und über die API erreichbar.

> **Vorschlag.** `ledger.Id = id;` vor dem `Update()` setzen oder bei Abweichung `400` zurückgeben, ein eigenes DTO nur mit den erlaubten Feldern verwenden und bei 0 betroffenen Zeilen `404` liefern.

---

### F3 — Doppelklick auf „Buchen“ bucht zweimal

**Schwere:** 🟠 mittel

Im Browser gemessen, mit Kontrollversuch, auf einem frischen Konto mit Saldo 1000:

| Aktion | Buchungen in der DB | Saldo danach | Meldung |
|---|---:|---:|---|
| **Ein** Doppelklick, Betrag 77 | 2 | 1000 → 846 | einmal „Buchung erfolgreich.“ |
| **Ein** Einfachklick, Betrag 55 | 1 | 846 → 791 | „Buchung erfolgreich.“ |

Die Daten bleiben konsistent — es sind zwei gültige Buchungen, das Total stimmt und die Historie passt. Der Fehler liegt in der Oberfläche: Die Benutzerin bucht ungewollt doppelt und sieht nur eine Erfolgsmeldung.

**Ursache.** `book()` prüft zu Beginn nicht, ob schon eine Anfrage läuft. `isSubmitting.set(true)` wirkt auf `[disabled]="isSubmitting()"` erst nach dem nächsten Change-Detection-Lauf; ein schneller zweiter Klick kommt vorher durch.

```ts
public book(): void {
    if (this.isSubmitting()) { return; }   // fehlt
    ...
```

> **Vorschlag.** Diese eine Zeile an den Anfang von `book()`. `deleteLedger()` hat dieselbe Lücke über `deletingId`; dort entschärft sie der `confirm()`-Dialog, sauber wäre dieselbe Prüfung.

---

### F4 — `/ledgers` ist ohne Login erreichbar

**Schwere:** 🟠 mittel

In `app.routes.ts` ist `AuthGuard` importiert, aber bei der Route `/ledgers` fehlt `canActivate: [AuthGuard]`. `ng lint` sagt es direkt:

```text
app.routes.ts  4:10  error  'AuthGuard' is defined but never used
```

Im Browser bestätigt: `http://localhost:4200/ledgers` ohne Login zeigt die Überschrift „Ledgers“, das Buchungsformular und die Löschen-Buttons, ohne Weiterleitung auf `/login`.

Es fliessen dabei keine Daten ab — `getLedgers()` liefert ohne Token ein `Observable`, das nie etwas sendet, und die API antwortet auf alles mit `401`. Die Seite ist aber bedienbar, und jede Aktion endet in einer unverständlichen Fehlermeldung. Nebenbei: Dieses leere `Observable` wird auch nie abgeschlossen, die Komponente wartet ewig darauf.

> **Vorschlag.** `canActivate: [AuthGuard]` bei `/ledgers` eintragen. Bei fehlendem Token in `getLedgers()` besser `throwError(...)` oder `of([])` zurückgeben statt eines Observables, das nie etwas liefert.

---

### F5 — Unbekanntes Konto liefert `204` statt `404`

**Schwere:** ⚪ klein

```http
GET /api/v1/Ledgers/999999            -> 204 No Content  (leer)
GET /api/v1/Ledgers/-1                -> 204 No Content  (leer)
GET /api/v1/Bookings?ledgerId=999999  -> 404 {"message":"Konto nicht gefunden."}
```

`Ledger? Get(int id)` gibt `null` zurück, woraus ASP.NET `204` macht. Beim Historie-Endpunkt macht ihr es richtig.

> **Vorschlag.** `IActionResult` verwenden und `NotFound(new { message = "Konto nicht gefunden." })` zurückgeben.

---

### F6 — Ungültiges JSON bricht euren API-Vertrag

**Schwere:** ⚪ klein

Euer Vertrag (#6) sagt: Fehler immer als `{ "message": "..." }`. Bei Typfehlern greift aber die automatische Prüfung von `[ApiController]`, und es kommt `ProblemDetails` ohne `message` zurück:

```http
POST /Bookings {"sourceId":1,"destinationId":2,"amount":"abc"}  -> 400 ProblemDetails
POST /Bookings {"sourceId":1,"destinationId":2,"amount":1e30}   -> 400 ProblemDetails
POST /Bookings (leerer Body)                                    -> 400 ProblemDetails
```

Das Frontend zeigt dann nur „Buchung fehlgeschlagen.“ Beim Historie-Endpunkt habt ihr genau dafür schon die Lösung gebaut, indem ihr `ledgerId` als `string` entgegennehmt und selbst parst.

Positiv: `{}` mit fehlenden Feldern wird korrekt mit eurer eigenen Meldung abgelehnt, und ein `amount` jenseits von `decimal` wird sauber abgefangen statt in einen 500 zu laufen.

> **Vorschlag.** `InvalidModelStateResponseFactory` in `Program.cs` so konfigurieren, dass auch Bindungsfehler als `{ message }` zurückkommen.

---

### F7 — Beträge unter einem Rappen werden angenommen

**Schwere:** ⚪ klein

```http
POST /Bookings {"sourceId":1,"destinationId":2,"amount":0.0000001}  -> 200 OK
```

Danach stehen Salden wie `6932.9999999` in der Liste. Das Total bleibt korrekt, es ist also kein Transaktionsproblem, sieht in einer Bank-App aber falsch aus. Das Eingabefeld hat zwar `step="0.01"`, geprüft wird im Code aber nur `amount <= 0` im Frontend und `amount > 0` im `BookingValidator`. Ein Betrag mit 40 Nachkommastellen wird stillschweigend gerundet.

> **Vorschlag.** Im `BookingValidator` zusätzlich auf höchstens zwei Nachkommastellen prüfen: `decimal.Round(amount, 2) != amount` → `InvalidAmount`.

---

### F8 — Lint ist rot, die CI prüft es nicht

**Schwere:** ⚪ klein

`npx ng lint` meldet zwei Fehler: den ungenutzten `AuthGuard` aus F4 und eine Prettier-Abweichung in `ledger.component.ts:112`. Eure `.gitlab-ci.yml` hat nur den `pages`-Job für mdBook — Build, Lint und Tests laufen in der CI nie. Deshalb ist F4 nicht aufgefallen, obwohl der Linter es direkt anzeigt.

> **Vorschlag.** Einen Job mit `npm ci && npm run lint && npm run build` und einen mit `dotnet test` ergänzen.

---

## Geprüft und in Ordnung

Das hier hat gehalten, inklusive eurer eigenen Nachweise.

| Prüfung | Ergebnis |
|---|---|
| `dotnet test Backend/Bank.Tests` | **20/20 grün** — IT-B1–B7, IT-D1–D4, IT-H1–H5, UT-V1–V3 |
| NBomber-Lasttest | **PASS** — 1500 Anfragen, 1387 × `200`, 113 × `409`, 0 × 5xx, Total vorher = nachher, Prüfungen a) b) c) d) d2) alle grün |
| 20 gleichzeitige Buchungen à 100 von einem Konto mit 1000 | genau **10 × `200`, 10 × `409`**, Saldo exakt `0`, genau 10 Historien-Einträge, Total unverändert |
| Löschen gegen gleichzeitige Buchung, **25 Runden** | 24 × (`204` löschen / `404` buchen), 1 × (`409` löschen / `200` buchen). Kein Geld verloren, nie auf ein gelöschtes Konto gebucht, kein 5xx |
| Löschen-Regeln | `401` ohne Token, `403` als `testuser`, `404` unbekannt, `409` „Konto hat noch Guthaben.“, `409` „Konto hat Buchungen.“, `204` gültig, `404` beim zweiten Mal |
| Buchungs-Validierung | `400` bei Betrag `0` und `-50`, `400` Quelle = Ziel, `404` unbekanntes Konto, `409` zu wenig Guthaben |
| Historie | `400` ohne `ledgerId`, `400` bei `abc`, `404` unbekannt, Einträge korrekt bei Quelle und Ziel |
| JWT | Rolle im Token auf `Administrators` manipuliert → `401`, Unsinns-Token → `401`, ohne Token → `401`. Signaturprüfung greift |
| `GET /Ledgers` unter Sperr-Konflikt | Zeile in `Ledgers` extern gesperrt, `innodb_lock_wait_timeout` auf 2 s. Die Retry-Schleife wiederholt korrekt und liefert nach dem Commit **9 Konten, 9 verschiedene IDs** — keine Duplikate, obwohl `allLedgers` zwischen den Versuchen nicht geleert wird |

---

## Von euch bereits dokumentiert — bestätigt

Alle vier Punkte aus eurem Protokoll treffen zu. Wir führen sie nur der Vollständigkeit halber auf.

- Kein Logout-Button vorhanden.
- `testuser` sieht die Löschen-Buttons — im Test sieben Stück. Der Klick endet korrekt mit „Keine Berechtigung zum Löschen.“ (Backend `403`).
- `auth.interceptor.ts` gibt das JWT bei jedem Request per `console.log` aus.
- Nach einer abgelehnten Buchung wird die Kontenliste nicht neu geladen. Im Browser bestätigt: Die Liste blieb nach „Zu wenig Guthaben auf dem Quellkonto.“ unverändert. Im Erfolgsfall ruft ihr `loadLedgers()` auf, im Fehlerfall nicht.

---

## F1 nachstellen

```bash
# Setup gemäss README, Backend starten, dann als admin einloggen
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"adminpass"}' | jq -r .token)

# Unvollständiger Body ohne "name" -> name wird NULL
curl -i -X PUT http://localhost:5000/api/v1/Ledgers/4 \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"id":4,"balance":50}'

# Kontenliste abrufen -> kommt nie zurück
curl -m 15 http://localhost:5000/api/v1/Ledgers -H "Authorization: Bearer $TOKEN"

# Aufräumen: Name in der DB zurücksetzen UND Bank.Web neu starten
#   UPDATE Ledgers SET name='Testkonto leer' WHERE id=4;
```

---

## Priorität aus unserer Sicht

1. **F1 zuerst** — ein einziger Request legt die Anwendung dauerhaft lahm, und die Lösung habt ihr im `BookingRepository` schon.
2. **F3 und F4** sind beide mit je einer Zeile erledigt.
3. **F2** danach, weil derselbe Code F1 auslöst.
4. F5 bis F8 sind Kosmetik und API-Hygiene.

Die Transaktionssicherheit selbst — Transaktion, `SELECT … FOR UPDATE`, feste Sperr-Reihenfolge, Retry und Historie in derselben Transaktion — mussten wir nicht anfassen. Die hält, was ihr im README beschreibt.
