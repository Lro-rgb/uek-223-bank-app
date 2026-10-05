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

## Links

- [Unterlagen](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app)
- [Bewertungskriterien](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/LB_223.html)
- [Übersicht](https://pages.ict-learnfactory.ch/ict-learnfactory/223/bank-app/Uebersicht.html)

## Unterlagen lokal bearbeiten

Braucht [Rust](https://rust-lang.org/tools/install/) und [mdBook](https://rust-lang.github.io/mdBook/index.html):

```
cargo install mdbook
cd Lerninhalte
mdbook serve --open
```
