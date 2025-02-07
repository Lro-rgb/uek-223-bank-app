# Die Bank: Das Problem

## Ausgangslage

Du hast das erstellte Programm der Bank zum Laufen gebracht und
analysiert. In diesem Teil geht es um die Multiuserprobleme.

### Teilaufgabe 1: Buchungen

Um den Betrieb zu simulieren, soll das Banking-Programm so erweitert werden,
dass es den Betrieb simuliert und zufällige Buchungen vornimmt.
Erweitere das Programm so in `Simple.cs` an der Stelle wo

```csharp	
////////////////////
// Your Code Here
////////////////////
```

steht, dass folgender Ablauf implementiert wird:

1. Ausgabe von «Booking, press ESC to stop.».
2. Um ein zufälliges Konto auszusuchen, sollte `allLedgers` in ein Array
umgewandelt werden.
3. Solange nicht «ESC» gedrückt wird:
    - `from` soll ein zufälliges Konto sein.
    - `to` soll ein zufälliges Konto sein.
    - `amount` soll ein zufälliger Betrag zwischen 1 und 100 sein.
    - `Ledgers.Book(amount, from, to);`
    - `Console.Write(".");`
4. Einen Zeilenumbruch auf der Konsole am Schluss.

Der Ablauf sollte so aussehen:

![Ablauf Programm](2024-11-21-09-53-50.png)

Lass das Programm laufen. Verändert sich die Summe des Geldes im
System? Sollte sie sich verändern?

### Teilaufgabe 2: Das erste Problem

Um mehrere User (multiuser) zu simulieren, starte das Programm mehrfach (mindestens drei Mal).
Je nach IDE musst du zudem die Build Config anpassen und mehrere parallele Instanzen erlauben, via der checkbox "Allow multiple instances".

![allow multiple instances](multiple-instances.png)

Verändert sich sie Summe des Geldes im System? Sollte sie sich verändern?

Warum verändert sich die Summe des Geldes? Nimm an, dass es drei Konten gibt:

Konti («Ledger a» mit 1000, «Ledger b» mit 1000 und «Ledger c» mit 0) und
lass das Programm zweimal laufen («Instanz 1» und «Instanz 2»).

Vorüberlegung: Wenn das eine Programm «Instanz a» die Daten in der
Datenbank ändert, ändern sie sich dann in den Ledger-Objekten des zweiten
gestarteten Programmes «Instanz b»?

Fülle die Daten zu folgendem Ablauf aus (exakt so, wie es das Programm
machen würde):

Start:

- Datenbank, Geld auf Ledger a: 1000
- Datenbank, Geld auf Ledger b: 1000
- Datenbank, Geld auf Ledger c: 0
- Datenbank, Geld Total:


Schritt 1: Instanz a und Instanz b lesen die Werte aus der Datenbank

- Programm Instanz a, «balance» von Ledger a: 1000
- Programm Instanz a, «balance» von Ledger b: 1000
- Programm Instanz a, «balance» von Ledger c: 0
- Programm Instanz b, «balance» von Ledger a: 1000
- Programm Instanz b, «balance» von Ledger b: 1000
- Programm Instanz b, «balance» von Ledger c: 0
- Datenbank, Geld auf Ledger a: 1000
- Datenbank, Geld auf Ledger b: 1000
- Datenbank, Geld auf Ledger c: 0
- Datenbank, Geld Total:


Schritt 2: Instanz a bucht 10 von Ledger a auf Ledger b (und speichert a und b)

- Programm Instanz a, «balance» von Ledger a:
- Programm Instanz a, «balance» von Ledger b:
- Programm Instanz a, «balance» von Ledger c:
- (HERE LIE DRAGONS, denke gut nach)
- Programm Instanz b, «balance» von Ledger a:
- Programm Instanz b, «balance» von Ledger b:
- Programm Instanz b, «balance» von Ledger c:
- Datenbank, Geld auf Ledger a:
- Datenbank, Geld auf Ledger b:
- Datenbank, Geld auf Ledger c:
- Datenbank, Geld Total:


Schritt 3: Instanz b bucht 20 von Ledger a auf Ledger c (und speichert a und c)

- Programm Instanz a, «balance» von Ledger a:
- Programm Instanz a, «balance» von Ledger b:
- Programm Instanz a, «balance» von Ledger c:
- Programm Instanz b, «balance» von Ledger a:
- Programm Instanz b, «balance» von Ledger b:
- Programm Instanz b, «balance» von Ledger c:
- Datenbank, Geld auf Ledger a:
- Datenbank, Geld auf Ledger b:
- Datenbank, Geld auf Ledger c:
- Datenbank, Geld Total (💣):

Wie könnte dieses Problem behoben werden?

### Teilaufgabe 3: Beheben des Problems

Ergänze `LedgerRepository.cs` um eine Methode `public void LoadBalance(Ledger ledger)`, welche mit
dem SQL-Befehl `SELECT balance FROM {Ledger.CollectionName} WHERE id=@Id` die Balance
aus der Datenbank liest. Schau bei `LedgerRepository.GetTotalMoney()` nach,
wie man einen Wert aus der Datenbank ausliest.
Ergänze `LedgerRepository.Book(…)` wie folgt.
    
```csharp
public void Book(decimal amount, Ledger from, Ledger to)
{
    this.LoadBalance(from);
    from.Balance -= amount;
    this.Update(from);
    // Complicate calculations
    Thread.Sleep(250);
    this.LoadBalance(to);
    to.Balance += amount;
    this.Update(to);
}
```	

Das sollte das erkannte Problem beheben?

Starte das Programm mehrfach und prüfe, ob nun kein Geld mehr
verschwindet oder erzeugt wird.

**Houston, we have a problem…**