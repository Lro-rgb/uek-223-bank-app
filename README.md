# Bank-App – ÜK 223

Gruppe: Matti & Luis

Bank-App aus dem ÜK 223 (Backend, Frontend und Kursunterlagen). Doku und Fortschritt geben wir im LearningView ab.

## Setup

1. `.env.sample` nach `.env` kopieren und die Passwörter setzen.
2. Datenbank starten: `docker compose up -d`
3. In `Backend/Bank.Web` und `Backend/Bank.Cli` jeweils `appsettings.sample.json` nach `appsettings.json` kopieren und den Connection-String mit den Werten aus der `.env` füllen:
   ```
   Server=localhost;Port=3306;Database=bank;User=bank_user;Password=<MARIADB_PASSWORD>;
   ```
   In `Bank.Web` zusätzlich einen eigenen `PrivateKey` setzen (mind. 32 Zeichen).
4. Alles Weitere zu Backend und Frontend steht in den READMEs der jeweiligen Ordner.

`.env` und `appsettings.json` sind im `.gitignore` und werden nicht committet.

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
| Transaktion mit Isolationslevel (Buchung als eine Einheit) | Halbe Buchung | offen: `BookingRepository.Book()` | offen: Lasttest mit NBomber (`Bank.Tests.Loadtest.Cli`), Total vorher = Total nachher |
| Saldo-Prüfung in der Transaktion mit `SELECT ... FOR UPDATE` | Saldo unter 0, Lost Update | offen: `BookingRepository.Book()` | offen: Integrationstests „Betrag grösser als Saldo wird nicht gebucht (409 Conflict)“ und „Buchung genau auf 0 klappt“, Lasttest: kein Saldo unter 0 |
| Retry-Schleife bei Deadlock (1213) oder Lock-Wait-Timeout (1205), feste Sperr-Reihenfolge | Abgebrochene Transaktionen gehen verloren | offen: `BookingRepository.Book()` (Vorbild: Schleife in `LedgerRepository.GetAllLedgers()`) | offen: Lasttest mit mehreren parallelen Usern: keine 500er, Total bleibt gleich, Anzahl Retries wird geloggt |
| Optimistic Locking (Concurrency-Token) im Zusatz-Feature | Lost Update beim gleichzeitigen Bearbeiten eines Kontos | offen: Zusatz-Feature (z.B. Konto umbenennen) | offen: Unit-Test „zweites Speichern mit altem Stand gibt Konflikt“ |

### Isolationslevel für Buchen

Wir sperren beide Konten mit `SELECT ... FOR UPDATE` (kleinere ID zuerst) und nutzen dafür Read Committed, das wir beim Start der Transaktion explizit setzen (MariaDB-Default ist Repeatable Read). Die Sperre sorgt dafür, dass niemand anders das Konto zwischen Lesen und Schreiben ändert, und durch die feste Reihenfolge gibt es kaum Deadlocks.

Serializable (so wie in den Kursunterlagen) wäre auch sicher, sperrt aber schon beim normalen Lesen. Wenn zwei User dasselbe Konto lesen und danach schreiben wollen, gibt es bei gleichzeitigen Buchungen auf dasselbe Konto häufig einen Deadlock, und die Retry-Schleife muss oft neu starten.

## Links

- [Unterlagen](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app)
- [Bewertungskriterien](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/LB_223.html)
- [Übersicht](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/Uebersicht.html)

## Tests

- [Manuelle Testfälle](docs/manuelle-testfaelle.md)

## Unterlagen lokal bearbeiten

Braucht [Rust](https://rust-lang.org/tools/install/) und [mdBook](https://rust-lang.github.io/mdBook/index.html):

```
cargo install mdbook
cd Lerninhalte
mdbook serve --open
```
