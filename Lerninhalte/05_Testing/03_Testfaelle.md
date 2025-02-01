# Testfälle

Gerade für nicht erwartete Probleme hilft es, menschliche Intelligenz zur Seite zu haben. Das heisst, es braucht Testfälle und Tests für kompetente Tester:innen.

Nehmen Sie an, es existieren folgende Anforderungen:

1. Nur Benutzer:innen in der Rolle «Administrators» oder «Users» können alle Ledgers sehen.
2. Bei einer Buchung wird ein Geldbetrag von einem Konto auf ein anderes verschoben.
3. Nur Benutzer:innen in der Rolle «Administrators» können Ledgers verändern.
4. Bei einer Buchung darf der Kontostand nie unter 0 fallen.
5. ...

Gehen Sie davon aus, dass Ihre Applikation nach den Anforderungen entwickelt wurde und keine Features fehlen.


## Aufgabenstellung

Sie sollen für die Softwarequalitätssicherung Testfälle erstellen.

### Teilaufgabe 1: Übersicht

Sie beschliessen, als Erstes einmal kurz aufzuschreiben, was Sie alles testen lassen möchten.

Machen Sie eine Liste in der Form:

- Testfall 1: Buchung in der Rolle «Administrators»
- Testfall 2: ...

Tipps:

- Jeder Testfall hat genau eine Anforderung, die er möglichst spezifisch testen soll. Das ist manchmal schwierig
- Jede Anforderung muss von mindestens einem, kann aber von mehreren Testfällen getestet werden.
- Es soll sich immer um ein beobachtbares Verhalten handeln.
- Sie haben keinen Zugriff auf den Quellcode oder die Entwicklungsumgebung.
- Testen Sie auch was nicht möglich sein darf.
- Testen Sie auch potenzielle Probleme bei der Datenintegrität bei gleichzeitigem Zugriff.
- Sie sollten mindestens 6 sinnvolle Testfälle finden, 9 ist realistischer.

Welche Tests eignen sich besser als explorative Testfälle, weil sie keine klaren ein- und Ausgaben haben?


### Teilaufgabe 2: Testfälle
Formulieren Sie die Testfälle korrekt aus, also mit Testfallnummer, getestete Anforderung, konkreter Eingabe und konkreter Ausgabe.

### Teilaufgabe 3: Exploratives Testen
Schreiben Sie eine Kurze aber klare Anleitung für das explorative Testen in der Form:
- Zu testende Anforderungen
- Rahmenbedingungen: Auf was soll geachtet werden
- Testideen