# Manuelle Testfälle

## Anforderungen

| Nr. | Anforderung |
|---|---|
| A1 | Nur Benutzer:innen in der Rolle „Administrators“ oder „Users“ können alle Ledgers sehen. |
| A2 | Benutzer:innen in der Rolle „Administrators“ oder „Users“ können buchen. Dabei wird ein Geldbetrag von einem Konto auf ein anderes verschoben. |
| A3 | Nur Benutzer:innen in der Rolle „Administrators“ können Ledgers direkt verändern (Name, Saldo). Buchen ist davon ausgenommen (siehe A2). |
| A4 | Bei einer Buchung darf der Kontostand nie unter 0 fallen. |
| A5 | Der Buchungsbetrag muss grösser als 0 sein. |
| A6 | Jede erfolgreiche Buchung erscheint genau einmal in der Historie, abgelehnte Buchungen gar nicht. |

A1–A4 stammen aus den Kursunterlagen (Testing → Testfälle), A5 ist eine eigene Anforderung.

## Vorbereitung

- DB, Backend (`http://localhost:5000`) und Frontend (`http://localhost:4200`) laufen.
- Testuser: `admin` (Administrators) und `testuser` (Users), Passwörter siehe `DatabaseSeeder.cs`.
- API-Aufrufe ohne Oberfläche mit Bruno oder Swagger (`http://localhost:5000`).
- Vor jedem Testfall Saldi und Total notieren (Ledger-Liste im Frontend).

## Testfälle

### TF-01 Buchung als Administrators

| | |
|---|---|
| **Anforderung** | A2 |
| **Vorbedingung** | Als `admin` eingeloggt. Konto X hat mindestens 100. |
| **Schritte** | 1. Saldo von X und Y notieren. 2. Im Frontend 100 von X auf Y buchen. 3. Ledger-Liste neu laden. |
| **Erwartetes Ergebnis** | Erfolgsmeldung. X hat 100 weniger, Y hat 100 mehr. Total unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-02 Buchung als Users

| | |
|---|---|
| **Anforderung** | A2 |
| **Vorbedingung** | Als `testuser` eingeloggt. Konto X hat mindestens 10. |
| **Schritte** | 1. Saldi notieren. 2. Im Frontend 10 von X auf Y buchen. 3. Ledger-Liste neu laden. |
| **Erwartetes Ergebnis** | Erfolgsmeldung im Frontend, im Network-Tab 200. X hat 10 weniger, Y hat 10 mehr. Total unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-03 Ledger ohne Rechte ändern

| | |
|---|---|
| **Anforderung** | A3 |
| **Vorbedingung** | Token von `testuser` in Bruno. |
| **Schritte** | 1. `PUT /api/v1/Ledgers/1` mit neuem Namen und Saldo senden. 2. Als `admin` die Ledger-Liste ansehen. |
| **Erwartetes Ergebnis** | Antwort 403 Forbidden. Ledger 1 unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-04 Ledgers ohne Login abrufen

| | |
|---|---|
| **Anforderung** | A1 |
| **Vorbedingung** | Kein Token. |
| **Schritte** | 1. `GET /api/v1/Ledgers` ohne `Authorization`-Header senden. |
| **Erwartetes Ergebnis** | Antwort 401 Unauthorized, keine Ledger-Daten. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-05 Buchung genau auf 0

| | |
|---|---|
| **Anforderung** | A4 |
| **Vorbedingung** | Als `admin` eingeloggt. Saldo von X notiert (z.B. 975). |
| **Schritte** | 1. Genau den ganzen Saldo von X auf Y buchen. 2. Ledger-Liste neu laden. |
| **Erwartetes Ergebnis** | Buchung erfolgreich. X hat 0, Y entsprechend mehr. Total unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-06 Betrag grösser als Saldo

| | |
|---|---|
| **Anforderung** | A4 |
| **Vorbedingung** | Als `admin` eingeloggt. Saldo von X notiert. |
| **Schritte** | 1. Saldo von X + 1 auf Y buchen. 2. Ledger-Liste neu laden. |
| **Erwartetes Ergebnis** | Fehlermeldung im Frontend, im Network-Tab 409. X und Y unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-07 Ungültiger Betrag

| | |
|---|---|
| **Anforderung** | A5 |
| **Vorbedingung** | Als `admin` eingeloggt. |
| **Schritte** | 1. Betrag 0 von X auf Y buchen. 2. Betrag -50 von X auf Y buchen (im Frontend und per Bruno). 3. Ledger-Liste neu laden. |
| **Erwartetes Ergebnis** | Beide Buchungen abgelehnt (Frontend: Fehlermeldung, API: 400 Bad Request). Alle Saldi unverändert. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

### TF-08 Parallele Buchungen (explorativ)

| | |
|---|---|
| **Anforderung** | A4 (bei parallelen Zugriffen) |
| **Art** | Explorativer Test, keine fixe Ein- und Ausgabe |
| **Vorbedingung** | Zwei Browser (oder ein normales und ein privates Fenster), beide als `admin` eingeloggt. Total notiert. |
| **Schritte** | 1. In beiden Fenstern eine Buchung vom selben Konto X vorbereiten, zusammen mehr als der Saldo von X. 2. Möglichst gleichzeitig absenden. 3. Mehrmals wiederholen, auch mit kleinen Beträgen und vertauschten Konten. 4. Ledger-Liste neu laden. |
| **Weitere Testidee** | Zwei Browser treffen die Race Condition selten. Deshalb zusätzlich viele `POST /api/v1/Bookings` gleichzeitig senden, z.B. mit mehreren Bruno-Runnern gleichzeitig oder einem kleinen Skript (mehrere `curl`-Aufrufe parallel), und danach Saldi und Total prüfen. |
| **Erwartetes Ergebnis** | Höchstens so viel abgebucht, wie auf X war. Kein Saldo unter 0. Total unverändert. Keine Fehlerseite (500). |
| **Beobachtungen** | |
| **Status** | |

### TF-09 Abgelehnte Buchung erzeugt keinen Historien-Eintrag

| | |
|---|---|
| **Anforderung** | A6 |
| **Vorbedingung** | Eingeloggt. Saldo von Konto X bekannt. Historie von X und Y sichtbar. |
| **Schritte** | 1. Buchung von X auf Y mit Betrag grösser als Saldo von X absenden. 2. Saldi von X und Y ansehen. 3. Historie von X und Y ansehen. |
| **Erwartetes Ergebnis** | Fehlermeldung im UI. Beide Saldi unverändert. Kein neuer Eintrag in der Historie. |
| **Tatsächliches Ergebnis** | |
| **Status** | |

## Anleitung exploratives Testen

Für Fehler ohne klare Ein- und Ausgabe, vor allem bei parallelen Zugriffen. Pro Session ca. 20 Minuten, Beobachtungen direkt notieren (was gemacht, was passiert, Screenshot bei Auffälligkeiten).

### Zu testende Anforderungen

- Parallele Zugriffe und ihre Auswirkung auf die Datenintegrität (A2, A4, A6)
- Fehlerhafte und manipulierte Eingaben an Formular und API (A5)
- Unvorhergesehene Benutzeraktionen (Doppelklick, Zurück-Button, abgelaufenes Token)
- Rechte der Rollen (A1, A3)

### Rahmenbedingungen

- Rolle: einmal als `admin`, einmal als `testuser`
- Daten: Total und Saldi vor jeder Session notieren, mindestens ein Konto mit Saldo 0 („Testkonto leer“)
- API unter Last: Backend läuft lokal, parallel kann der NBomber-Lasttest laufen
- Werkzeuge: Browser mit Network-Tab, Bruno oder Swagger für direkte API-Aufrufe

### Testideen

- Mehrere Benutzer buchen gleichzeitig vom selben Konto (2 Browser, Bruno-Runner oder Skript), danach Total und Saldi prüfen
- Mit Bruno ungültige Werte senden: negativer Betrag, Betrag 0, sehr grosser Betrag, Text statt Zahl, nicht existierende Konto-ID, fehlendes Feld
- Buchen ohne Token, mit manipuliertem Token und als `testuser` auf Endpunkte für Administrators
- „Buchen“ mehrmals schnell hintereinander klicken: entsteht die Buchung nur einmal?
- Konten-Liste in Tab 1 offen lassen, in Tab 2 buchen, dann in Tab 1 mit veralteten Saldi buchen
- Backend während einer Buchung stoppen: verständliche Meldung im UI, kein halber Zustand in der DB
