# 223 Bank App

Willkommen beim Ük 223! In diesem Repository findest du den Code für die Applikation, die wir im Modul 223 erstellen werden, sowie die Lerninhalte.

Dieses Repository dient als Ablage für die bereitgestellten Informationen. Gewisse Informationen befinden sich auch im LearningView.
Ebenfalls wird wie gewohnt im LearningView die Dokumentation der Arbeit abgegeben, sowie der Fortschritt dokumentiert.

## Setup

Kopiere `.env.example` in `.env` und passe die Werte an.  
Danach kannst du die Datenbank starten mit `docker compose up -d`.

Beachte auch die READMEs in den einzelnen Projekten beim Setup der Umgebung.

## Informationen

- [Unterlagen](https://pages.ict-learnfactory.ch/223/bank-app/)
- [Bewertungskriterien](./Lerninhalte/src/LB_223.md)
- [Übersicht](./Lerninhalte/src/Uebersicht.md)

## Unterlagen bearbeiten

1. [Rust installieren](https://rust-lang.org/tools/install/)
2. `cargo install mdbook`
3. `mdbook serve --open` im Projekt Pfad (`Lerninhalte`) ausführen

---

[Dokumentation mdBook](https://rust-lang.github.io/mdBook/index.html)
