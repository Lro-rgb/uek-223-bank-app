# Testprotokoll für externe Tester

Dieses Protokoll ist für eine Person aus einem anderen Team. Sie richtet die App nur mit dem [README](../README.md) und diesem Protokoll selbst ein und testet sie. Die ausführlichen Testfälle stehen in den [manuellen Testfällen](manuelle-testfaelle.md).

Ausgefüllt vom externen Tester am 09.10.2026 (Abschnitte 1–6). Der erste, freie Test desselben Teams vom 08.10.2026 steht im [Testbericht des externen Teams](testbericht-extern.md). Abschnitt 7 und die Hinweise „Umgesetzt“ stammen von uns.

## 1. Angaben

| | |
|---|---|
| **Tester:in (Name, Team)** | Henry Roesler Schellenberg, Team Noel / Henry / Maksym |
| **Datum** | 09.10.2026 |
| **Commit** (`git log -1 --format=%h`) | `b9947ba` |
| **Umgebung** | Ubuntu 24.04 unter WSL2 (Windows), Chromium 131 (headless, über Playwright gesteuert) |
| **Versionen** | `docker 29.1.3`, `dotnet 9.0.318`, `node v24.11.1` |

## 2. Einrichtung nach README

| Schritt (README) | Hat geklappt? | Probleme, Unklarheiten |
|---|---|---|
| Setup 1: `.env` anlegen | ja | `.env.sample` endet ohne Zeilenumbruch, dadurch klebt die letzte Zeile beim `cat` an der nächsten Ausgabe. Nur kosmetisch. |
| Setup 2: `docker compose up -d` | **nein** | Zwei Probleme, siehe unten. |
| Setup 3: `appsettings.json` in Bank.Web und Bank.Cli | ja, mit Abweichung | Der Connection-String im README gibt `Port=3306` fest vor. Wegen Problem 2b musste ich auf `Port=3307` wechseln. |
| Starten: Backend auf Port 5000 | ja | — |
| Starten: Frontend auf Port 4200 | ja | `npm install` und `npm start` ohne Auffälligkeiten. |
| Login als `admin` und als `testuser` | ja | Beide `200`, Passwörter standen wie beschrieben im README. |
| Tests: `dotnet test Backend/Bank.Tests` | ja | **26 / 26 bestanden** |

**Problem 2a — `docker compose` existiert nicht auf jedem Docker-Setup.**

```
$ docker compose up -d
unknown shorthand flag: 'd' in -d
```

Auf dieser Maschine läuft Docker Engine 29.1.3 aus den Ubuntu-Paketen, ohne das Compose-v2-Plugin. Das README nennt unter „Voraussetzungen“ nur „Docker“. Vorschlag: „Docker inkl. Compose v2 (`docker compose version` muss funktionieren)“ und ein Hinweis auf `docker-compose-plugin`.

**Problem 2b — Port und Container-Name sind fest verdrahtet.**

In `docker-compose.yml` stehen `container_name: "bank-db"` und `3306:3306` fix. Auf meiner Maschine lief bereits die Bank-App eines anderen 223er-Teams mit genau demselben Container-Namen und Port. Ein externer Tester aus dem Kurs hat damit sehr wahrscheinlich eine Kollision. Vorschlag: Port über die `.env` steuerbar machen, z.B. `- "${MARIADB_PORT:-3306}:3306"`, und im README den Connection-String darauf beziehen.

**Workaround für diesen Test:** Container von Hand gemäss `docker-compose.yml` gestartet (gleiches Image, gleiche Env-Variablen, `db-init` gemountet), nur mit Name `bank232-db` und Port `3307`. `db-init/01-test-db.sql` lief dabei korrekt, `bank` und `bank_test` waren danach vorhanden.

## 3. Vorbereitung

Frische DB (Volume neu angelegt, entspricht `docker compose down -v` + `up -d`), danach `Bank.Web` gestartet.

> **Hinweis für weitere Durchläufe (nach diesem Test ergänzt):** „Testkonto leer“ in TF-01 bis TF-09 nicht als Zielkonto verwenden, damit ZF-1 ohne neue DB möglich ist (siehe Hinweis zu ZF-1).

| Konto | Saldo zu Beginn |
|---|---|
| Manitu AG | 8 004.00 |
| Chrysalkis GmbH | 426.00 |
| Smith & Co KG | 2 823.00 |
| Testkonto leer | 0.00 |
| **Total** | **11 253.00** |

Buchungen zu Beginn: 0.

## 4. Testfälle

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| TF-01 | Als `admin` 100 von Konto X auf Y buchen | Erfolgsmeldung, X −100, Y +100, Total unverändert | 100 von Manitu AG (8 004.00) auf Chrysalkis GmbH (426.00). Meldung „Buchung erfolgreich.“, Network `POST bookings → 200`. Danach Manitu 7 904.00, Chrysalkis 526.00, Total 11 253.00. | **bestanden** |
| TF-02 | Als `testuser` 10 von X auf Y buchen | Erfolgsmeldung (Network 200), X −10, Y +10, Total unverändert | Als `testuser` 10 von Manitu AG auf Smith & Co KG. Meldung „Buchung erfolgreich.“, Network `POST bookings → 200`. Danach Manitu 7 894.00, Smith & Co 2 833.00, Total 11 253.00. | **bestanden** |
| TF-03 | Mit Token von `testuser` `PUT /api/v1/Ledgers/1` mit neuem Namen und Saldo senden | 403 Forbidden, Ledger 1 unverändert | Body `{"name":"Manipuliert","balance":999999}` mit `testuser`-Token: **403 Forbidden**. Danach als `admin` gelesen: `{"id":1,"name":"Manitu AG","balance":7894.00}` — unverändert. | **bestanden** |
| TF-04 | `GET /api/v1/Ledgers` ohne `Authorization`-Header | 401 Unauthorized, keine Daten | **401 Unauthorized**, Body leer, keine Ledger-Daten. | **bestanden** |
| TF-05 | Als `admin` genau den ganzen Saldo von X auf Y buchen | Erfolgreich, X hat 0, Total unverändert | Ganzer Saldo 526.00 von Chrysalkis GmbH auf Smith & Co KG. Meldung „Buchung erfolgreich.“, `200`. Danach Chrysalkis **0.00**, Smith & Co 3 359.00, Total 11 253.00. | **bestanden** |
| TF-06 | Als `admin` Saldo von X + 1 auf Y buchen | Fehlermeldung, Network 409, Saldi unverändert | 3 360 von Smith & Co KG (Saldo 3 359.00) auf Manitu AG. Meldung „Zu wenig Guthaben auf dem Quellkonto.“, Network `POST bookings → 409`. Smith & Co 3 359.00 und Manitu 7 894.00 unverändert. | **bestanden** |
| TF-07 | Betrag 0 und -50 buchen (Frontend und API) | Frontend: Fehlermeldung, API: 400, Saldi unverändert | **Frontend:** beide Male „Der Betrag muss grösser als 0 sein.“, **kein Request** im Network-Tab (Prüfung im Frontend). **API:** `POST /api/v1/Bookings` mit 0 und -50 je **400** mit `{"message":"Der Betrag muss grösser als 0 sein."}`. Alle Saldi unverändert, Total 11 253.00. | **bestanden** |
| TF-08 | Explorativ: zwei Browserfenster als `admin`, gleichzeitig vom selben Konto buchen, zusammen mehr als der Saldo | Höchstens der Saldo wird abgebucht, kein Saldo unter 0, Total unverändert, keine 500 | Siehe Beobachtungen unten. Kein Saldo unter 0, Total durchgehend 11 253.00, kein einziger 5xx. | **bestanden** |
| TF-09 | Historie von X und Y öffnen, dann Betrag grösser als Saldo von X buchen | Fehlermeldung, Saldi unverändert, kein neuer Eintrag in der Historie | Historie Manitu AG (894.00) = 12 Einträge, Smith & Co KG (1 359.00) = 3 Einträge. Dann 895 von Manitu auf Smith: „Zu wenig Guthaben auf dem Quellkonto.“, `409`. Saldi unverändert, Historien weiterhin **12** und **3**. | **bestanden** |

### TF-08 — Beobachtungen im Detail

**Zwei Fenster.** Zwei Chromium-Kontexte, beide als `admin`. Fenster A: 2 000 von Smith & Co KG (3 359.00) auf Manitu AG. Fenster B: 2 000 von Smith & Co KG auf Chrysalkis GmbH. Beide Klicks gleichzeitig ausgelöst.

- Fenster A: „Buchung erfolgreich.“, `POST bookings → 200`
- Fenster B: „Zu wenig Guthaben auf dem Quellkonto.“, `POST bookings → 409`
- Danach Smith & Co 1 359.00, Manitu 9 894.00, **Total 11 253.00**

**Weitere Testidee (Skript).** 20 gleichzeitige `POST /api/v1/Bookings`, je 1 000 von Manitu AG (Saldo 9 894.00) auf Testkonto leer:

- **9 × `200`, 11 × `409`**, kein 5xx
- 9 × 1 000 = 9 000 — genau das Maximum, das auf 9 894.00 passt, danach Manitu 894.00
- Testkonto leer 9 000.00, **Total 11 253.00**

**Auffällig (deckt sich mit eurer bekannten Einschränkung):** Fenster B zeigte nach der Ablehnung weiterhin die alten Saldi (Smith & Co 3 359.00, Manitu 7 894.00), bis die Seite neu geladen wurde. Im Erfolgsfall wird die Liste neu geladen, im Fehlerfall nicht.

### Zusatz-Features

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| ZF-1 | Als `admin` „Testkonto leer“ löschen (frische DB, Saldo 0, keine Buchungen) | Meldung „Konto … gelöscht.“, Konto verschwindet | Auf einer zweiten frischen DB ausgeführt (Begründung unten). „Testkonto leer“ mit Saldo 0.00 und ohne Buchungen gelöscht: Meldung **„Konto "Testkonto leer" gelöscht.“**, Network `DELETE ledgers/4 → 204`, Konto aus der Liste verschwunden. Total 21 339.00 vorher wie nachher. | **bestanden** |
| ZF-2 | Als `admin` ein Konto mit Saldo > 0 löschen | 409 „Konto hat noch Guthaben.“, Konto bleibt | Manitu AG (1 017.00) gelöscht: Meldung **„Konto hat noch Guthaben.“**, `DELETE ledgers/1 → 409`. Konto bleibt mit 1 017.00 in der Liste. | **bestanden** |
| ZF-3 | Als `admin` ein Konto mit Saldo 0, das schon Buchungen hat, löschen | 409 „Konto hat Buchungen.“, Konto bleibt | Chrysalkis GmbH (Saldo 0.00 nach TF-05, mit Buchungen) gelöscht: Meldung **„Konto hat Buchungen.“**, `DELETE ledgers/2 → 409`. Konto bleibt. | **bestanden** |
| ZF-4 | In der Buchungshistorie ein Konto wählen, dann auf dieses Konto buchen | Neue Buchung erscheint sofort zuoberst, genau einmal | Historie von Manitu AG geöffnet (12 Einträge), dann 123 von Smith & Co KG auf Manitu AG gebucht. Ohne Neuladen: **13 Einträge**, zuoberst `09.10.2026 10:17:38 | Smith & Co KG | Manitu AG | $123.00`. In der DB genau **eine** Buchung über 123. | **bestanden** |

**Hinweis zu ZF-1.** Die Vorbedingung lautet „frische DB, Saldo 0, keine Buchungen“. In meinem Durchlauf hatte „Testkonto leer“ nach TF-08 einen Saldo von 9 000.00 und Buchungen, weil ich es dort als Zielkonto verwendet hatte. ZF-1 habe ich deshalb nach TF-09 auf einer neu aufgesetzten DB ausgeführt, damit die Vorbedingung wirklich erfüllt war. **Vorschlag fürs Protokoll:** ZF-1 vor die TF-Reihe stellen oder dazuschreiben, dass „Testkonto leer“ in TF-01 bis TF-09 nicht als Zielkonto verwendet werden soll.

## 5. Endzustand

| | |
|---|---|
| **Total am Ende** | **11 253.00** — identisch mit dem Start (11 253.00). Nach TF-01 bis ZF-4 standen Manitu AG 1 017.00, Chrysalkis GmbH 0.00, Smith & Co KG 1 236.00, Testkonto leer 9 000.00, bei 14 Buchungen. Beim separaten ZF-1-Lauf: 21 339.00 vorher wie nachher, obwohl ein Konto gelöscht wurde (Saldo war 0). |
| **Konto unter 0?** | Nein. Kleinster Saldo zu jedem Zeitpunkt 0.00. |
| **Fehler 500 oder Absturz?** | Nein. Im Backend-Log keine einzige unbehandelte Exception und kein 5xx, auch nicht während der 20 parallelen Buchungen. |

**Ergebnis: 13 / 13 bestanden** (TF-01 bis TF-09, ZF-1 bis ZF-4).

## 6. Weitere Beobachtungen

### Nachtest der Befunde aus dem externen Testbericht

Die acht Befunde aus `docs/testbericht-extern.md` sind alle behoben. Gegengeprüft auf Commit `b9947ba`:

| Befund | Nachtest | Ergebnis |
|---|---|---|
| F1 | `PUT /Ledgers/1` mit Teil-Body `{"id":1,"balance":7159}` (ohne `name`) | `400 {"message":"Name fehlt."}` — Name bleibt erhalten |
| F1 | `PUT /Ledgers/1` mit `"name": null` | `400 {"message":"Name fehlt."}` |
| F1 | `name` **direkt in der DB** auf `NULL` gesetzt, dann `GET /Ledgers` | `200` nach **0.004 s** mit `"name": null` — kein Hänger mehr. Auch `GET /Ledgers/1` antwortet sofort. Das ist die wichtigere Hälfte des Fix: Auch Altbestand mit `NULL` legt die App nicht mehr lahm. |
| F2 | `PUT /Ledgers/3` mit Body `{"id":2,...}` | `400 {"message":"ID in URL und Body stimmen nicht überein."}` — beide Konten unverändert |
| F3 | Ein Doppelklick auf „Buchen“ | genau **1** `POST bookings`, genau **1** Buchung in der DB |
| F4 | `http://localhost:4200/ledgers` ohne Login | Weiterleitung auf `/login` |
| F5 | `GET /Ledgers/999999` | `404 {"message":"Konto nicht gefunden."}` |
| F6 | `POST /Bookings` mit `"amount":"abc"` | `400 {"message":"Ungültige Eingabe. Bitte die Werte prüfen."}` — hält jetzt den API-Vertrag ein |
| F7 | `POST /Bookings` mit `"amount":0.0000001` | `400 {"message":"Der Betrag darf höchstens 2 Nachkommastellen haben."}` |
| F8 | `npx eslint src`, `.gitlab-ci.yml` | Lint sauber (Exit 0). CI hat jetzt einen `frontend`-Job (`npm ci`, `lint`, `build`) und einen `backend-test`-Job (`dotnet test` gegen eine eigene MariaDB). |

### Bekannte Einschränkungen — bestätigt

Die drei im README genannten Punkte treffen weiterhin zu und sind sauber dokumentiert: kein Logout, `testuser` sieht die Löschen-Buttons und bekommt beim Klick `403`, und `auth.interceptor.ts` gibt das JWT per `console.log` aus. Dazu kommt die in TF-08 beobachtete veraltete Kontenliste nach einer abgelehnten Buchung.

### Was ich verbessern würde

1. **Setup portabel machen** (Problem 2a und 2b). Port und Container-Name über die `.env` steuerbar machen und unter „Voraussetzungen“ Compose v2 explizit nennen. Das ist der einzige Punkt, an dem ich ohne eigenes Zutun nicht weitergekommen wäre.
2. **Kontenliste auch im Fehlerfall neu laden.** In `ledger.component.ts` ruft nur der `next`-Zweig `loadLedgers()` auf. Ein `loadLedgers()` auch im `error`-Zweig würde die veralteten Saldi aus TF-08 beheben und kostet eine Zeile.
3. **ZF-1 im Protokoll nach vorne ziehen** oder „Testkonto leer“ in den TF-Fällen als Zielkonto ausschliessen, siehe Hinweis oben.
4. **Kleinigkeit:** `.env.sample` mit Zeilenumbruch abschliessen.

**Umgesetzt (#20):** Port, Container- und Projektname über die `.env` steuerbar, Compose v2 im README unter „Voraussetzungen“ (1). Kontenliste wird nach einer abgelehnten Buchung neu geladen (2). Hinweis zu „Testkonto leer“ in Abschnitt 3 (3). `.env.sample` endet mit Zeilenumbruch (4).

### Einschätzung

Die Transaktionssicherheit ist die Stärke dieses Projekts. In TF-05, TF-06, TF-08 und TF-09 hat sich das System in jedem Fall korrekt verhalten: Das Total blieb über den ganzen Durchlauf exakt bei 11 253.00, kein Saldo wurde negativ, abgelehnte Buchungen hinterliessen keinen Historien-Eintrag, und unter 20 gleichzeitigen Anfragen ging genau so viel durch, wie der Saldo hergab. Die `SELECT … FOR UPDATE`-Sperre mit fester Reihenfolge und der Retry tun, was das README beschreibt.

## 7. Besprechung der Ergebnisse

| | |
|---|---|
| **Datum** | 09.10.2026 |
| **Teilnehmende** | Luis Rosado, Matti Näf (Team Matti & Luis); Henry Roesler Schellenberg, Noel Kohn (externes Testteam) |
| **Ergebnis** | 13 / 13 bestanden, alle Befunde F1–F8 aus dem Test vom 08.10.2026 im Nachtest behoben |
| **Erkenntnisse** | Die Transaktionssicherheit hält auch im geführten Durchlauf (Total durchgehend 11 253.00, kein Saldo unter 0, kein 5xx). Schwachstelle war die Einrichtung: Container-Name und Port waren fest, und Compose v2 wurde nicht als Voraussetzung genannt. |
| **Massnahmen** | Setup portabel gemacht, Kontenliste nach Ablehnung neu laden, Hinweis zu ZF-1 im Protokoll, `.env.sample` korrigiert (alle #20, !23). Bewusst offen: kein Logout, `testuser` sieht die Löschen-Buttons (Backend sperrt mit 403), `console.log` mit dem Token (README, Bekannte Einschränkungen). |
