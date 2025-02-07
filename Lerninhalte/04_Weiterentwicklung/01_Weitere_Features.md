# Weiterentwicklung (Fallbeschreibung)

Die Bank hat nun ein grobes Back- und Frontend. Es fehlen jedoch noch einige Features, um die Anwendung nutzbar zu machen.

Im ersten Schritt bauen wir noch die "Buchen"-Funktion ein. Diese soll es ermöglichen, dass ein User eine Buchung tätigen kann.

## Teilaufgabe 1: Analyse

Die "Buchen"-Funktion soll einen gegebenen Betrag von einem Konto auf ein anderes übertragen. Zusätzlich soll es nicht möglich sein, mit einer Buchung den Kontostand unter 0 zu bringen. Du hast folgende Operationen zur Verfügung:

- **Betrag von Konto X lesen**
- **Betrag von Konto X ändern**
- **Konto X in der Datenbank ändern**

Schreibe den Ablauf im Backend auf, wenn du einen Betrag B von Konto "Quelle" auf Konto "Ziel" übertragen möchtest.

- Wo kann es bei diesem Ablauf in Multiuser-Applikationen zu Problemen kommen?  
    - Finde mindestens drei Probleme.
- Was ist die Lösung dieses Problems?

## Teilaufgabe 2: Implementation

- **Backend:**  
  Implementiere den Controller und das Repository.

- **Transaktionen:**  
  Wenn eine Transaktion misslingt, solltest du sie neu starten. Dafür musst du die ganze Operation in eine Schleife einbauen.

- **Testen:**  
  Teste das Backend mit der Bruno, indem du dich zuerst einloggst und dann den Bookings-API-Endpunkt aufrufst.

- **Frontend:**  
  Implementiere den API-Service und teste die Buchung in der Applikation.

## Weitere Features

Nun bist du gefragt: Baue weitere Features ein, die du für sinnvoll hältst, z. B.:

- Konto löschen
- Konto erstellen
- Konto umbenennen
- Kontoübersicht
- …

Diese weiteren Features werden als Teil der Bank bewertet.