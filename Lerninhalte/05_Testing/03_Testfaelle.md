# Testfälle

Gerade für nicht erwartete Probleme hilft es, menschliche Intelligenz zur Seite zu haben. Das heisst, es braucht Testfälle und Tests für kompetente Tester:innen.

Nimm an, es existieren folgende Anforderungen:

1. Nur Benutzer:innen in der Rolle „Administrators“ oder „Users“ können alle Ledgers sehen.  
2. Bei einer Buchung wird ein Geldbetrag von einem Konto auf ein anderes verschoben.  
3. Nur Benutzer:innen in der Rolle „Administrators“ können Ledgers verändern.  
4. Bei einer Buchung darf der Kontostand nie unter 0 fallen.  
5. …  

Gehe davon aus, dass deine Applikation nach diesen Anforderungen entwickelt wurde und keine Features fehlen.

## Aufgabenstellung

Du sollst für die Softwarequalitätsicherung Testfälle erstellen.

### Teilaufgabe 1: Übersicht

Schreibe zunächst auf, was du alles testen lassen möchtest. Erstelle eine Liste in der Form:

- **Testfall 1:** Buchung in der Rolle „Administrators“
- **Testfall 2:** Buchung in der Rolle „Users“
- **Testfall 3:** Versuch, ein Ledger ohne ausreichende Rechte zu verändern
- **Testfall 4:** Buchung, die den Kontostand exakt auf 0 reduziert
- **Testfall 5:** Buchung, bei der der Betrag größer ist als die verfügbare Balance
- **Testfall 6:** Datenintegrität bei parallelen Zugriffen auf Ledgers  
  *(Weitere Testfälle sind denkbar.)*

**Tipps:**

- Jeder Testfall hat genau eine Anforderung, die er möglichst spezifisch testen soll. Das kann manchmal schwierig sein.
- Jede Anforderung muss von mindestens einem, kann aber von mehreren Testfällen getestet werden.
- Es soll sich immer um ein beobachtbares Verhalten handeln.
- Du hast keinen Zugriff auf den Quellcode oder die Entwicklungsumgebung.
- Teste auch, was nicht möglich sein darf.
- Teste auch potenzielle Probleme bei der Datenintegrität bei gleichzeitigem Zugriff.
- Du solltest mindestens 6 sinnvolle Testfälle finden – 9 sind realistischer.

Überlege, welche Tests sich besser als explorative Testfälle eignen, weil sie keine klaren Ein- und Ausgaben haben.

### Teilaufgabe 2: Testfälle

Formuliere die Testfälle korrekt aus, indem du folgende Punkte angibst:

- **Testfallnummer:** z. B. Testfall 1  
- **Getestete Anforderung:** z. B. Nur Benutzer:innen in der Rolle „Administrators“ können Ledgers verändern  
- **Konkrete Eingabe:** z. B. Administrator loggt sich ein und versucht, Ledger X zu ändern  
- **Konkrete Ausgabe:** z. B. Ledger X wird erfolgreich geändert, oder eine Erfolgsmeldung erscheint

### Teilaufgabe 3: Exploratives Testen

Erstelle eine kurze, aber klare Anleitung für das explorative Testen in der folgenden Form:

- **Zu testende Anforderungen:**  
  Definiere, welche Anforderungen du explorativ testen möchtest, z. B.  
  - Unvorhergesehene Benutzeraktionen  
  - Fehlerhafte Eingaben  
  - Parallele Zugriffe und deren Auswirkungen auf die Datenintegrität

- **Rahmenbedingungen:**  
  Beschreibe, worauf du achten sollst, z. B.  
  - Rolle des Benutzers  
  - Vorhandene Daten in der Datenbank  
  - Stabilität der API unter Last  
  - Reaktion auf ungültige Eingaben

- **Testideen:**  
  Notiere konkrete Ideen, wie du vorgehen möchtest, um unerwartete Probleme zu identifizieren, z. B.  
  - Simuliere mehrere Benutzer, die gleichzeitig Buchungen durchführen  
  - Versuche, mit ungültigen oder manipulativen Daten die API zu stressen  
  - Beobachte das Systemverhalten, wenn absichtlich Fehler provoziert werden (z. B. fehlerhafte Authentifizierungsdaten, falsche Ledger-IDs)

---

Viel Erfolg bei der Erstellung deiner Testfälle!