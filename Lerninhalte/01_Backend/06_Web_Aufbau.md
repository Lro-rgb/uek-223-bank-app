# Weiterentwicklung

Du hast erkannt, dass es für Multiuser-Applikationen meistens Transaktionen und ein Backend braucht. Diese Applikation implementiert einen grossen Teil davon.

## Aufgabenstellung

Mit dem Backend haben wir uns ja bereits mehrfach auseinander gesetzt. Schau dir jetzt auch mal das `.Web` Projekt an.

Schau dir das Projekt (zusammen mit `Core` und `DbAccess`) an und beantworte folgende Fragen:

- **Architekturmuster:**  
  Wenn du die Verzeichnisnamen anschaust, welchem Architekturmuster folgt diese Applikation?

- **Controllers:**  
  Was befindet sich im Verzeichnis „Controllers“ und was ist dessen Aufgabe?

- **Models:**  
  Was befindet sich im Verzeichnis „Models“ und was ist dessen Aufgabe?

- **Datenbank:**  
  Wo wird die Datenbank erstellt und die Daten geseeded?

- **Benutzer:**  
  Welche Benutzer mit welchen Passwörter und Rollen werden erstellt?

- **DataGrip:**  
  Schau dir mit DataGrip die erstellten Tabellen und Daten an.

- **LoginController:**  
  Welche URL hat der Login-API-Controller-Endpunkt und welche Methode wird verwendet?

- **BankInfoController:**  
  Welche URL hat der BankInfo-API-Controller-Endpunkt und welche Methode wird verwendet?

- **LedgersController:**  
  Welche URL hat die Liste aller ledgers im API-Controller-Endpunkt und welche Methode wird verwendet?

- **Authentifizierung:**  
  Wo werden der Benutzername und das Passwort überprüft?

## Teilaufgabe 1: Authentifizierung

- Verwende Bruno, um Frontend-Aufrufe zu simulieren.
    - Eine Vorlage findest du [hier](https://gitlab.ict-learnfactory.ch/223/bruno)
    - Erstell ein neues Projekt in deiner Gruppe um die API Collection zu teilen.
- Logge dich ein, indem du ein JWT anforderst.
- Kopiere das JWT.
- Fordere nun eine Liste aller Ledgers an.
- Mache den gleichen Aufruf ohne das Token im Header – was passiert?
- Rufe auch den Infopunkt mit und ohne Token auf – was ist der Unterschied?  
  **A:** Mit Token wird die Benutzerinfo zurückgegeben.

## Teilaufgabe 2: Generelles Verständnis des Backends

- Verfolge die Aufrufe von `/api/v1/login`, `/api/v1/bankinfo` und `/api/v1/ledgers` durch die Applikation. Du kannst dafür auch die Debuggingfunktion verwenden.
- Schreibe Fragen und Unklarheiten auf und versuche, diese mit deinen Mitlernenden oder dem Coach zu klären.