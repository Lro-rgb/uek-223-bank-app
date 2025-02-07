# Authentifizierung

Diese Multiuserapp braucht Authentifizierung und Autorisierung.

- **Authentifizierung** ist der Prozess, bei dem die Identität einer Person überprüft wird.
- **Autorisierung** ist der Prozess, bei dem die Berechtigungen einer Person überprüft werden.

Theoretisch könnte ein User eingeloggt (authentifiziert) sein, aber keine Berechtigung (autorisierung) haben, um auf bestimmte Daten zuzugreifen.

## Aufgabenstellung

Finde heraus, wie die Authentifizierung und die Autorisierung gelöst wurden.

## Teilaufgabe 1: Authentifizierung

Beantworte anhand des Quellcodes folgende Fragen:

- **Findet eine Authentifizierung statt?**  
  Prüfe, ob eine Authentifizierung im Code implementiert ist.

- **Wo und wie findet die Authentifizierung statt?**  
  Untersuche, an welcher Stelle und auf welche Weise die Authentifizierung erfolgt.

- **Welche Art von Authentifizierung wird verwendet?**  
  Welche Technologie kommt zum Einsatz?

- **Wo werden den Benutzer:innen die Rollen zugewiesen und wo sind sie gespeichert?**  
  Finde heraus, wo die Rollen festgelegt und gespeichert werden.

- **Wie wird sichergestellt, dass sich ein User nicht selbst in eine Rolle hinzufügen kann?**  
  Analysiere, welche Mechanismen dies verhindern.

## Teilaufgabe 2: Authentifizierung

Ändere im Quellcode temporär die Rechte auf die Liste aller Ledger, sodass nur Benutzer:innen in der Rolle "Administrators" Zugriff erhalten.

Benutze Bruno ([Vorlage](https://gitlab.ict-learnfactory.net/223/bruno)) und führe folgende Schritte aus:

1. **Ohne Authentifizierung:**  
   Rufe die Liste aller Ledgers ohne Authentifizierung auf. Was passiert?

2. **Mit korrekter Rolle:**  
   Logge dich als ein User mit der korrekten Rolle ein und rufe die Liste aller Ledgers auf. Was passiert?

3. **Mit unberechtigter Rolle:**  
   Logge dich als ein User mit einer Rolle ohne Rechte ein und rufe die Liste aller Ledgers auf. Was passiert?