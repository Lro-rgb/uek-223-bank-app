# Testprotokoll für externe Tester

Dieses Protokoll ist für eine Person aus einem anderen Team. Sie richtet die App nur mit dem [README](../README.md) und diesem Protokoll selbst ein und testet sie. Die ausführlichen Testfälle stehen in den [manuellen Testfällen](manuelle-testfaelle.md).

Ausgefüllt mit den Angaben aus dem [Testbericht des externen Teams](testbericht-extern.md). Was dort nicht steht, ist mit „keine Angabe“ oder „nicht durchgeführt“ markiert.

## 1. Angaben

| | |
|---|---|
| **Tester:in (Name, Team)** | Noel Kohn, Maksym Shepetko, Henry Roesler Schellenberg (externes Team) |
| **Datum** | 08.10.2026 |
| **Commit** (`git log -1 --format=%h`) | `0f7a6ed` (`main`) |
| **Umgebung** (Betriebssystem, Browser) | Betriebssystem: keine Angabe. Browser: Chromium. Backend `:5000`, Frontend `:4200` |
| **Versionen** (`docker --version`, `dotnet --version`, `node -v`) | MariaDB 11.6.2. Docker, .NET und Node: keine Angabe |

## 2. Einrichtung nach README

Bitte nur mit dem README arbeiten und nicht nachfragen. Was unklar war, hier notieren.

| Schritt (README) | Hat geklappt? (ja / nein) | Probleme, Unklarheiten |
|---|---|---|
| Setup 1: `.env` anlegen | ja | Keine gemeldet. Bericht: Datenbank „frisch aufgesetzt nach README“. |
| Setup 2: `docker compose up -d` | ja | Keine gemeldet (MariaDB 11.6.2 lief). |
| Setup 3: `appsettings.json` in Bank.Web und Bank.Cli | Bank.Web: ja. Bank.Cli: keine Angabe | Keine gemeldet. Bank.Web lief auf Port 5000. |
| Starten: Backend auf Port 5000 | ja | Keine gemeldet |
| Starten: Frontend auf Port 4200 | ja | Keine gemeldet |
| Login als `admin` und als `testuser` (siehe README, Benutzer) | ja | Keine gemeldet. Login als `admin` per `curl`, `testuser` im Browser (sah die Löschen-Buttons). |
| Tests: `dotnet test Backend/Bank.Tests` (Anzahl bestanden / total) | ja | 20 / 20 |

## 3. Vorbereitung

- Frische DB: `docker compose down -v`, `docker compose up -d`, danach `Bank.Web` neu starten.
- Startzustand notieren: Saldi der 4 Konten und Total.
- Für API-Aufrufe (TF-03, TF-04, TF-07) Swagger unter `http://localhost:5000` oder `curl` verwenden. Ein Token gibt `POST /api/v1/Login` mit `{ "username": "...", "password": "..." }`.

| Konto | Saldo zu Beginn |
|---|---|
| Manitu AG | keine Angabe |
| Chrysalkis GmbH | keine Angabe |
| Smith & Co KG | keine Angabe |
| Testkonto leer | keine Angabe |
| **Total** | keine Angabe |

## 4. Testfälle

Status: **bestanden** / **nicht bestanden** / **nicht durchgeführt**. Bei „nicht bestanden“ bitte genau beschreiben, was passiert ist.

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| TF-01 | Als `admin` 100 von Konto X auf Y buchen | Erfolgsmeldung, X −100, Y +100, Total unverändert | Nicht als TF-01 protokolliert. Ähnlicher Versuch beim Befund F3: Einfachklick mit Betrag 55, Saldo 846 → 791, Meldung „Buchung erfolgreich.“ (Rolle: keine Angabe). | nicht durchgeführt |
| TF-02 | Als `testuser` 10 von X auf Y buchen | Erfolgsmeldung (Network-Tab 200), X −10, Y +10, Total unverändert | keine Angabe | nicht durchgeführt |
| TF-03 | Mit Token von `testuser` `PUT /api/v1/Ledgers/1` mit neuem Namen und Saldo senden | 403 Forbidden, Ledger 1 unverändert | keine Angabe. `PUT` wurde nur als `admin` getestet (Befunde F1 und F2). | nicht durchgeführt |
| TF-04 | `GET /api/v1/Ledgers` ohne `Authorization`-Header | 401 Unauthorized, keine Daten | Ohne Token antwortet die API auf alles mit 401, auch mit manipulierter Rolle oder Unsinns-Token. | bestanden |
| TF-05 | Als `admin` genau den ganzen Saldo von X auf Y buchen | Erfolgreich, X hat 0, Total unverändert | Über die API statt im Frontend: 20 gleichzeitige Buchungen à 100 von einem Konto mit 1000, danach Saldo genau 0, Total unverändert. | bestanden |
| TF-06 | Als `admin` Saldo von X + 1 auf Y buchen | Fehlermeldung, Network-Tab 409, Saldi unverändert | API: 409 bei zu wenig Guthaben. Browser: Meldung „Zu wenig Guthaben auf dem Quellkonto.“, Kontenliste unverändert. | bestanden |
| TF-07 | Betrag 0 und -50 buchen (Frontend und API) | Frontend: Fehlermeldung, API: 400, Saldi unverändert | API: 400 bei Betrag 0 und -50. Frontend: keine Angabe. | bestanden (nur API) |
| TF-08 | Explorativ: zwei Browserfenster als `admin`, gleichzeitig vom selben Konto buchen, zusammen mehr als der Saldo | Höchstens der Saldo wird abgebucht, kein Saldo unter 0, Total unverändert, keine 500 | Zwei Browserfenster: keine Angabe. Über die API: 20 gleichzeitige Buchungen à 100 von 1000, genau 10 × 200 und 10 × 409, Saldo 0, Total unverändert, kein 5xx. Löschen gegen gleichzeitige Buchung (25 Runden): kein Geld verloren, kein 5xx. | bestanden |
| TF-09 | Historie von X und Y öffnen, dann Betrag grösser als Saldo von X buchen | Fehlermeldung, Saldi unverändert, kein neuer Eintrag in der Historie | Über die API: bei 10 × 409 genau 10 Historien-Einträge (nur die 10 erfolgreichen). Lasttest d2) PASS. Historie im Frontend: keine Angabe. | bestanden |

**Zusatz-Features:**

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| ZF-1 | Als `admin` „Testkonto leer“ löschen (frische DB, Saldo 0, keine Buchungen) | Meldung „Konto … gelöscht.“, Konto verschwindet | Über die API: gültiges Löschen 204, beim zweiten Mal 404. Welches Konto: keine Angabe. | bestanden |
| ZF-2 | Als `admin` ein Konto mit Saldo > 0 löschen | 409 „Konto hat noch Guthaben.“, Konto bleibt | 409 „Konto hat noch Guthaben.“ | bestanden |
| ZF-3 | Als `admin` ein Konto mit Saldo 0, das schon Buchungen hat, löschen | 409 „Konto hat Buchungen.“, Konto bleibt | 409 „Konto hat Buchungen.“ | bestanden |
| ZF-4 | In der Buchungshistorie ein Konto wählen, dann auf dieses Konto buchen | Neue Buchung erscheint sofort zuoberst, genau einmal | keine Angabe. Historie über die API: Einträge korrekt bei Quelle und Ziel. | nicht durchgeführt |

## 5. Endzustand

| | |
|---|---|
| **Total am Ende** (muss gleich wie zu Beginn sein, ausser es wurde ein Konto mit Saldo 0 gelöscht) | Zahl: keine Angabe. Im Lasttest und in den Nebenläufigkeitstests blieb das Total gleich. Beim Befund F2 wurde ein Saldo per `PUT` von 0 auf 50 gesetzt (Geld entstanden, seit #20 nicht mehr möglich). |
| **Konto unter 0?** | nein |
| **Fehler 500 oder Absturz?** | Kein 5xx. Aber F1: Nach einem Konto ohne Namen hing `GET /api/v1/Ledgers` endlos, nur ein Neustart von `Bank.Web` half (behoben in #19). |

## 6. Weitere Beobachtungen

Was ist aufgefallen, was war unklar, was würdest du verbessern?

- F1 (kritisch): Konto mit `name` = NULL legt `GET /api/v1/Ledgers` lahm.
- F2: `PUT /api/v1/Ledgers/{id}` nimmt die ID aus dem Body, ein unvollständiger Body setzt den Namen auf NULL.
- F3: Doppelklick auf „Buchen“ bucht zweimal.
- F4: `/ledgers` ist ohne Login erreichbar.
- F5: Unbekanntes Konto bei `GET /api/v1/Ledgers/{id}` liefert 204 statt 404.
- F6: Ungültiges JSON liefert ProblemDetails statt `{ message }`.
- F7: Beträge unter einem Rappen werden angenommen.
- F8: `ng lint` ist rot, die CI prüft es nicht.
- Bestätigt aus unserem Protokoll: kein Logout, `testuser` sieht die Löschen-Buttons, `console.log` mit dem Token, Kontenliste wird nach einer abgelehnten Buchung nicht neu geladen.

Details: [Testbericht des externen Teams](testbericht-extern.md).

## 7. Besprechung der Ergebnisse

| | |
|---|---|
| **Datum** | 08.10.2026 |
| **Teilnehmende** | Luis Rosado, Matti Näf (Team Matti & Luis); Noel Kohn, Maksym Shepetko, Henry Roesler Schellenberg (externes Testteam) |
| **Ergebnis** (z.B. „x/13 bestanden“) | 9/13 bestanden (TF-07 nur API), 4 nicht durchgeführt (TF-01, TF-02, TF-03, ZF-4), 0 nicht bestanden. Dazu 8 Befunde ausserhalb der Transaktionslogik, davon 1 kritisch (F1). |
| **Erkenntnisse** | Die Transaktionssicherheit hält: 20/20 Tests, Lasttest PASS, eigene Nebenläufigkeitstests ohne Geldverlust, ohne negativen Saldo und ohne 5xx. Die Fehler lagen in Lesen, `PUT`, API-Vertrag und Oberfläche. |
| **Massnahmen** (was wir ändern, mit Issue/MR) | F1–F8 behoben (#19, !22). Lost Update über `PUT` verhindert und Kontenliste nach abgelehnter Buchung neu laden (#20). Bewusst offen: kein Logout, `testuser` sieht die Löschen-Buttons (Backend sperrt mit 403), `console.log` mit dem Token (README, Bekannte Einschränkungen). |
