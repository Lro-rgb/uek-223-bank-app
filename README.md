# Bank-App – ÜK 223

Gruppe: Matti & Luis

Bank-App aus dem ÜK 223 (Backend, Frontend und Kursunterlagen). Doku und Fortschritt geben wir im LearningView ab.

## Setup

1. `.env.sample` nach `.env` kopieren und die Passwörter setzen. `MARIADB_USER` muss `bank_user` bleiben, weil `db-init/01-test-db.sql` diesem User die Rechte auf die Test-Datenbank `bank_test` gibt.
2. Datenbank starten: `docker compose up -d`. Beim ersten Start legt `db-init` zusätzlich die Datenbank `bank_test` für die Integrationstests an.
3. In `Backend/Bank.Web` und `Backend/Bank.Cli` jeweils `appsettings.sample.json` nach `appsettings.json` kopieren und den Connection-String mit den Werten aus der `.env` füllen:
   ```
   Server=localhost;Port=3306;Database=bank;User=bank_user;Password=<MARIADB_PASSWORD>;
   ```
   In `Bank.Web` zusätzlich einen eigenen `PrivateKey` setzen (mind. 32 Zeichen).
4. Alles Weitere zu Backend und Frontend steht in den READMEs der jeweiligen Ordner.

`.env` und `appsettings.json` sind im `.gitignore` und werden nicht committet.

**Nach Änderungen an Tabellen oder Seed-Daten** (z.B. neue Spalte in einem Model) muss die lokale DB neu aufgebaut werden. Die App legt Tabellen nur an, wenn noch keine existieren (`EnsureCreated()`), und der Seed schreibt nur in leere Tabellen. `docker compose down -v` löscht dabei alle lokalen Daten:

```
docker compose down -v
docker compose up -d
```

Danach `Bank.Web` neu starten.

## Starten

Voraussetzungen: Docker, .NET 9 SDK und Node 22. Die Befehle laufen im Hauptordner des Repos, nach dem Setup oben.

```
docker compose up -d                       # Datenbank
dotnet run --project Backend/Bank.Web      # Backend auf http://localhost:5000
cd Frontend
npm install
npm start                                  # Frontend auf http://localhost:4200
```

Backend und Frontend laufen je in einem eigenen Terminal.

Tests: `dotnet test Backend/Bank.Tests`. Dafür muss die DB laufen, Bank.Web nicht. Details in den [Integrationstests](docs/integrationstests.md).

Login: `admin` (Rolle Administrators) und `testuser` (Rolle Users), die Passwörter stehen in `Backend/Bank.DbAccess/DatabaseSeeder.cs`.

## Analyse Mehrbenutzeranforderungen

### Ablauf einer Buchung

Betrag B wird von Konto Quelle auf Konto Ziel gebucht:

1. Betrag von Quelle lesen
2. Prüfen, ob Quelle mindestens B hat, sonst abbrechen
3. Betrag von Quelle um B verringern
4. Quelle in der DB ändern
5. Betrag von Ziel lesen
6. Betrag von Ziel um B erhöhen
7. Ziel in der DB ändern

Wie der Ablauf im Code umgesetzt ist, zeigt das [Sequenzdiagramm Buchen](docs/sequenzdiagramm-buchen.md), für das Zusatz-Feature Konto löschen das [Sequenzdiagramm Konto löschen](docs/sequenzdiagramm-loeschen.md).

Läuft das ohne Transaktion, kann zwischen jedem Schritt ein anderer User dieselben Konten lesen oder ändern. Genau dort entstehen die Probleme unten.

### Probleme

| Problem | Wo im Ablauf | Beispiel mit 2 Usern | Lösung |
|---|---|---|---|
| Lost Update | zwischen 1 und 4 (bzw. 5 und 7) | Quelle hat 100. A und B lesen beide 100. A bucht 30 weg und schreibt 70, B bucht 20 weg und schreibt 80. Am Schluss steht 80, die 30 von A sind weg, beim Ziel aber angekommen. Geld ist entstanden. | Lesen und Schreiben in einer Transaktion, Zeile beim Lesen sperren (`SELECT ... FOR UPDATE` oder Isolationslevel Serializable) |
| Saldo unter 0 | zwischen 2 und 4 | Quelle hat 50. A und B prüfen beide „50 ≥ 40“ und buchen je 40 weg, relativ geschrieben (`UPDATE ... SET balance = balance - 40`). Saldo -30, obwohl jede Prüfung für sich korrekt war. Mit absolutem Schreiben (`SET balance = 10`) wäre es stattdessen ein Lost Update. | Prüfung innerhalb derselben Transaktion auf der gesperrten Zeile, nicht vorher und nicht auf alten Daten |
| Halbe Buchung | zwischen 4 und 7 | A hat Quelle schon geändert, dann bricht das Programm oder die Verbindung ab. Ziel bekommt nichts, das Geld ist verschwunden. B sieht in der Zwischenzeit ein falsches Total. | Alle Schritte in einer Transaktion: entweder Commit von beidem oder Rollback |
| Deadlock / abgebrochene Transaktion | 1 und 5 | Entsteht erst durch das Sperren, also durch die Lösung der anderen Probleme. A bucht Konto 1 → 2, B gleichzeitig 2 → 1. A sperrt 1, B sperrt 2, beide warten aufeinander. Die DB bricht eine Transaktion ab. | Konten immer in fester Reihenfolge sperren (kleinere ID zuerst) und abgebrochene Transaktion in einer Schleife neu starten (Retry) |

### Massnahmen

| Massnahme | Verhindert | Wo implementiert | Test |
|---|---|---|---|
| Transaktion mit Isolationslevel (Buchung als eine Einheit) | Halbe Buchung | `BookingRepository.TryBook()`: eine Verbindung, `BeginTransaction(IsolationLevel.ReadCommitted)`, `Commit()` erst nach beiden Updates und dem Historien-Eintrag, sonst `Rollback()` | IT-B1, IT-B6 ([Integrationstests](docs/integrationstests.md)), NBomber Prüfung a) Total vorher = Total nachher |
| Saldo-Prüfung in der Transaktion mit `SELECT ... FOR UPDATE` | Saldo unter 0, Lost Update | `BookingRepository.SelectBalanceForUpdate()` sperrt beide Konten, die Prüfung `sourceBalance < amount` läuft danach in `TryBook()` auf der gesperrten Zeile | IT-B2, IT-B3, **IT-B5** (zwei gleichzeitige Buchungen: mit `FOR UPDATE` grün, ohne rot), IT-B6, NBomber Prüfung b) kein Saldo unter 0, TF-05, TF-06 |
| Retry-Schleife bei Deadlock (1213) oder Lock-Wait-Timeout (1205), feste Sperr-Reihenfolge | Abgebrochene Transaktionen gehen verloren | `BookingRepository.Book()`: Schleife mit `MaxAttempts = 3`, `IsTransient()` erkennt 1213/1205, fehlgeschlagene Versuche (1213/1205) werden als Warnung geloggt, das Aufgeben als Fehler, danach 409. Sperr-Reihenfolge in `TryBook()` (`Math.Min` vor `Math.Max`) | IT-B6 (2000 parallele Buchungen: Total gleich, kein Saldo unter 0, Erfolge = Einträge, `RetriesExhausted` wird nur gezählt), NBomber Prüfung c) keine 5xx, TF-08 |
| Historien-Eintrag (Tabelle `Bookings`) in derselben Transaktion wie die beiden Saldo-Updates. Keine Buchung ohne Eintrag, kein Eintrag ohne Buchung, auch bei Retry und Rollback. | Historie und Kontostände passen nicht zusammen (z.B. Eintrag einer zurückgerollten Buchung) | `BookingRepository.InsertBooking()` innerhalb von `TryBook()` vor dem `Commit()`, Anzeige über `BookingRepository.GetHistory()` (Zusatz-Feature 2) | IT-B4, IT-B6 (Anzahl Erfolge = Anzahl Einträge), IT-H1, IT-H2, NBomber Prüfung d) und d2), TF-09 |

### Isolationslevel für Buchen

Wir sperren beide Konten mit `SELECT ... FOR UPDATE` (kleinere ID zuerst) und nutzen dafür Read Committed, das wir beim Start der Transaktion explizit setzen (MariaDB-Default ist Repeatable Read). Die Sperre sorgt dafür, dass niemand anders das Konto zwischen Lesen und Schreiben ändert, und durch die feste Reihenfolge gibt es kaum Deadlocks.

Serializable (so wie in den Kursunterlagen) wäre auch sicher, sperrt aber schon beim normalen Lesen. Wenn zwei User dasselbe Konto lesen und danach schreiben wollen, gibt es bei gleichzeitigen Buchungen auf dasselbe Konto häufig einen Deadlock, und die Retry-Schleife muss oft neu starten.

### Zusatz-Features und Transaktionssicherheit

**Buchungshistorie (Zusatz-Feature 2):** Die Historie wird nur gelesen. Ein Eintrag entsteht ausschliesslich in der Buchungs-Transaktion (Massnahme 4). Eine zurückgerollte oder abgelehnte Buchung hinterlässt deshalb keinen Eintrag.

**Konto löschen (Zusatz-Feature 1):**

| Problem | Beispiel mit 2 Usern | Lösung |
|---|---|---|
| Löschen während einer Buchung | A prüft, ob Konto X leer ist (Saldo 0), B bucht gleichzeitig 50 auf X, danach löscht A das Konto. Die 50 sind verschwunden. | Löschen in einer Transaktion, X mit `SELECT ... FOR UPDATE` sperren und den Saldo auf der gesperrten Zeile prüfen. Buchen sperrt X ebenfalls, also wartet eine Operation auf die andere. |
| Konto mit Saldo > 0 löschen | Das Geld auf dem Konto fehlt danach im Total. | Nur löschen, wenn der Saldo 0 ist, sonst 409 Conflict mit Meldung. |
| Buchung auf ein gelöschtes Konto | B hat die Konten-Liste noch vor dem Löschen geladen und bucht auf X. | Backend prüft in der Buchungs-Transaktion, ob beide Konten existieren, sonst 404 (API-Vertrag #6). |
| Konto mit Einträgen in der Historie | Der Fremdschlüssel `Bookings` → `Ledgers` (Restrict) verhindert das Löschen, ohne Behandlung gibt es einen DB-Fehler (500). | Löschen mit 409 „Konto hat Buchungen“ ablehnen, die Historie bleibt erhalten. |

Wo implementiert: `LedgerRepository.Delete()` (eine Transaktion, Read Committed, `SELECT ... FOR UPDATE`, Retry bei 1213/1205 wie beim Buchen), `DELETE /api/v1/Ledgers/{id}` nur für Administrators. Test: [Integrationstests IT-D1 bis IT-D4](docs/integrationstests.md).

## Links

- [Unterlagen](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app)
- [Bewertungskriterien](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/LB_223.html)
- [Übersicht](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/Uebersicht.html)

## Tests

- [Manuelle Testfälle](docs/manuelle-testfaelle.md)
- [Integrationstests](docs/integrationstests.md)
- [Bruno-Collection](https://gitlab.ict-learnfactory.ch/ict-learnfactory/223/durchfuehrungen/223-ae-ims-24/gruppe-matti-luis-bruno) (API-Tests, eigenes Projekt)

### Welche Tests die Transaktionssicherheit prüfen

Massnahmen 1–4 siehe [Massnahmen](#massnahmen). Wie die Integrationstests gestartet werden, steht in den [Integrationstests](docs/integrationstests.md), der Lasttest unten unter [Lasttest Buchungen](#lasttest-buchungen-nbomber).

| Test | Was er prüft | Massnahme |
|---|---|---|
| IT-B5 mit `FOR UPDATE` | Zwei gleichzeitige Buchungen über 80 von einem Konto mit 100, die erste wird nach der Saldo-Prüfung angehalten: Die zweite wartet auf die Sperre und wird abgelehnt, Saldo 20, 1 Eintrag. | 2 |
| IT-B5 ohne `FOR UPDATE` | Gegenprobe: `FOR UPDATE` testweise entfernt, der Test wird rot (Saldo -60). Die Sperre ist die Ursache, nicht der Zufall. | 2 |
| IT-B6 | 20 parallele User × 100 Buchungen zwischen 2 Konten: Total gleich, kein Saldo unter 0, Anzahl Erfolge = Anzahl Einträge. `RetriesExhausted` wird nur gezählt. | 1, 2, 3, 4 |
| IT-H2 | Abgelehnte Buchung (zu wenig Saldo) hinterlässt keinen Historien-Eintrag. | 4 |
| IT-D1–D4 | Konto löschen in einer Transaktion mit `FOR UPDATE`: nur mit Saldo 0 und ohne Buchungen, sonst 409, unbekanntes Konto 404. Kein Geld verschwindet aus dem Total. | Zusatz-Feature 1, wie 1 und 2 |
| Lasttest a) | Total vorher = Total nachher nach 1500 parallelen Buchungen über die API. | 1, 2 |
| Lasttest b) | Kein Konto unter 0. | 2 |
| Lasttest c) | Keine 5xx unter paralleler Last. | 3 |
| Lasttest d) | Neue Booking-IDs = Anzahl 200er. | 4 |
| Lasttest d2) | Jede 200er-Buchung hat genau einen Eintrag (Quelle, Ziel, Betrag), keine 409 hat einen. | 4 |
| TF-08 | Explorativ: zwei Browser und 20 gleichzeitige `curl`-Buchungen vom selben Konto, kein Saldo unter 0, Total gleich, keine 500. | 2, 3 |
| TF-09 | Abgelehnte Buchung im Frontend: Saldi unverändert, kein neuer Eintrag in der Historie. | 4 |

### Lasttest Buchungen (NBomber)

`Backend/Bank.Tests.Loadtest.Cli` loggt sich als `admin` ein, liest alle Konten und schickt dann 30 Sekunden lang 50 Buchungen pro Sekunde an `POST /api/v1/bookings`. Jede Buchung geht von einem zufälligen Konto auf ein anderes, mit einem Betrag zwischen 1 und 500. Danach vergleicht das Programm die Konten mit dem Stand vorher und holt über `GET /api/v1/Bookings?ledgerId=` die Historie aller Konten vor und nach dem Lauf (jede Booking-ID nur einmal, weil eine Buchung bei Quelle und Ziel steht). Während des Laufs darf niemand sonst buchen. Last und Beträge sind als Konstanten oben in `Program.cs` einstellbar.

Starten (DB und Bank.Web müssen laufen):

```
cd Backend/Bank.Tests.Loadtest.Cli
dotnet run              # Buchungen
dotnet run -- bankinfo  # alter Lasttest auf /bankinfo
```

Der HTML-Report landet in `booking_reports/` (nicht im Repo). Ergebnis vom 07.10.2026:

```
Szenario: bookings
Anfragen total: 1500
Anfragen ok / fehlgeschlagen: 1500 / 0
Durchschnittsdauer (ok): 8.88 ms
Statuscodes: 200: 1409, 409: 91, 400: 0, 404: 0, 5xx: 0, andere: 0

Total vorher:  10541.00
Total nachher: 10541.00

PASS a) Total vorher = Total nachher
PASS b) kein Konto unter 0
PASS c) keine 5xx
PASS d) neue Booking-IDs (1409) = Anzahl 200er (1409)
PASS d2) neue Einträge entsprechen genau den 200er-Buchungen (Quelle, Ziel, Betrag)
     Jede der 1409 erfolgreichen Buchungen hat genau einen Eintrag, und es gibt keinen weiteren.
     Also hat keine der 91 abgelehnten Buchungen (409) einen Eintrag hinterlassen (Massnahme 4).

Ergebnis: PASS
```

Die 409-Antworten sind keine Fehler im System: Das Backend lehnt die Buchung ab, weil das Quellkonto zu wenig Saldo hat oder weil nach 3 Versuchen bei Deadlock/Lock-Wait-Timeout aufgegeben wurde. Im Bank.Web-Log gab es in diesem Lauf keine Retries (1213/1205), die 91 Conflicts waren also alle „zu wenig Saldo“.

d) allein zeigt nur, dass gleich viele neue Einträge wie 200er entstanden sind. d2) vergleicht zusätzlich Quelle, Ziel und Betrag jeder 200er-Anfrage mit den neuen Einträgen, damit kein Eintrag einer 409 einen fehlenden Eintrag einer 200 ausgleichen kann.

Belegte Massnahmen: Massnahme 2 (kein Saldo unter 0, Prüfung b), Massnahmen 1 und 2 (Total bleibt gleich, Prüfung a), Massnahme 3 (keine 5xx unter paralleler Last, Prüfung c), Massnahme 4 (genau ein Historien-Eintrag pro erfolgreicher Buchung, keiner für abgelehnte, Prüfungen d und d2).

## Unterlagen lokal bearbeiten

Braucht [Rust](https://rust-lang.org/tools/install/) und [mdBook](https://rust-lang.github.io/mdBook/index.html):

```
cargo install mdbook
cd Lerninhalte
mdbook serve --open
```
