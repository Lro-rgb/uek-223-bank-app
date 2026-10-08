# Testprotokoll für externe Tester

Dieses Protokoll ist für eine Person aus einem anderen Team. Sie richtet die App nur mit dem [README](../README.md) und diesem Protokoll selbst ein und testet sie. Die ausführlichen Testfälle stehen in den [manuellen Testfällen](manuelle-testfaelle.md).

## 1. Angaben

| | |
|---|---|
| **Tester:in (Name, Team)** | |
| **Datum** | |
| **Commit** (`git log -1 --format=%h`) | |
| **Umgebung** (Betriebssystem, Browser) | |
| **Versionen** (`docker --version`, `dotnet --version`, `node -v`) | |

## 2. Einrichtung nach README

Bitte nur mit dem README arbeiten und nicht nachfragen. Was unklar war, hier notieren.

| Schritt (README) | Hat geklappt? (ja / nein) | Probleme, Unklarheiten |
|---|---|---|
| Setup 1: `.env` anlegen | | |
| Setup 2: `docker compose up -d` | | |
| Setup 3: `appsettings.json` in Bank.Web und Bank.Cli | | |
| Starten: Backend auf Port 5000 | | |
| Starten: Frontend auf Port 4200 | | |
| Login als `admin` und als `testuser` (siehe README, Benutzer) | | |
| Tests: `dotnet test Backend/Bank.Tests` (Anzahl bestanden / total) | | |

## 3. Vorbereitung

- Frische DB: `docker compose down -v`, `docker compose up -d`, danach `Bank.Web` neu starten.
- Startzustand notieren: Saldi der 4 Konten und Total.
- Für API-Aufrufe (TF-03, TF-04, TF-07) Swagger unter `http://localhost:5000` oder `curl` verwenden. Ein Token gibt `POST /api/v1/Login` mit `{ "username": "...", "password": "..." }`.

| Konto | Saldo zu Beginn |
|---|---|
| Manitu AG | |
| Chrysalkis GmbH | |
| Smith & Co KG | |
| Testkonto leer | |
| **Total** | |

## 4. Testfälle

Status: **bestanden** / **nicht bestanden** / **nicht durchgeführt**. Bei „nicht bestanden“ bitte genau beschreiben, was passiert ist.

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| TF-01 | Als `admin` 100 von Konto X auf Y buchen | Erfolgsmeldung, X −100, Y +100, Total unverändert | | |
| TF-02 | Als `testuser` 10 von X auf Y buchen | Erfolgsmeldung (Network-Tab 200), X −10, Y +10, Total unverändert | | |
| TF-03 | Mit Token von `testuser` `PUT /api/v1/Ledgers/1` mit neuem Namen und Saldo senden | 403 Forbidden, Ledger 1 unverändert | | |
| TF-04 | `GET /api/v1/Ledgers` ohne `Authorization`-Header | 401 Unauthorized, keine Daten | | |
| TF-05 | Als `admin` genau den ganzen Saldo von X auf Y buchen | Erfolgreich, X hat 0, Total unverändert | | |
| TF-06 | Als `admin` Saldo von X + 1 auf Y buchen | Fehlermeldung, Network-Tab 409, Saldi unverändert | | |
| TF-07 | Betrag 0 und -50 buchen (Frontend und API) | Frontend: Fehlermeldung, API: 400, Saldi unverändert | | |
| TF-08 | Explorativ: zwei Browserfenster als `admin`, gleichzeitig vom selben Konto buchen, zusammen mehr als der Saldo | Höchstens der Saldo wird abgebucht, kein Saldo unter 0, Total unverändert, keine 500 | | |
| TF-09 | Historie von X und Y öffnen, dann Betrag grösser als Saldo von X buchen | Fehlermeldung, Saldi unverändert, kein neuer Eintrag in der Historie | | |

**Zusatz-Features:**

| Nr. | Testfall | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|
| ZF-1 | Als `admin` „Testkonto leer“ löschen (frische DB, Saldo 0, keine Buchungen) | Meldung „Konto … gelöscht.“, Konto verschwindet | | |
| ZF-2 | Als `admin` ein Konto mit Saldo > 0 löschen | 409 „Konto hat noch Guthaben.“, Konto bleibt | | |
| ZF-3 | Als `admin` ein Konto mit Saldo 0, das schon Buchungen hat, löschen | 409 „Konto hat Buchungen.“, Konto bleibt | | |
| ZF-4 | In der Buchungshistorie ein Konto wählen, dann auf dieses Konto buchen | Neue Buchung erscheint sofort zuoberst, genau einmal | | |

## 5. Endzustand

| | |
|---|---|
| **Total am Ende** (muss gleich wie zu Beginn sein, ausser es wurde ein Konto mit Saldo 0 gelöscht) | |
| **Konto unter 0?** | |
| **Fehler 500 oder Absturz?** | |

## 6. Weitere Beobachtungen

Was ist aufgefallen, was war unklar, was würdest du verbessern?

-

## 7. Besprechung der Ergebnisse

| | |
|---|---|
| **Datum** | |
| **Teilnehmende** | |
| **Ergebnis** (z.B. „x/13 bestanden“) | |
| **Erkenntnisse** | |
| **Massnahmen** (was wir ändern, mit Issue/MR) | |
