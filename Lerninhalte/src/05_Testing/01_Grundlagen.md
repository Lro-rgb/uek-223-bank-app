# Testing

Wir vermuten, dass die Funktion `book(…)` im `BookingRepository` im Backend anfällig für Multiuserprobleme sein könnte. Darum erstellen wir Unittests, um speziell die Multiuserfähigkeiten und potenzielle Probleme zu testen. Um Multiuserprobleme zu prüfen, müssen normale Unittests zuerst das Vertrauen erhöhen, dass die Applikation gemäss Spezifikation funktioniert.

## Aufgabenstellung

- **Erstelle** Integrationstests mit einer guten Test Coverage.
- **Erstelle** danach Tests, die speziell die Multiuserprobleme testen.

In der aktuellen Applikation gibt es sehr wenig Logik, daher ist ein Unit-Test nicht sinnvoll. Wir erstellen deshalb Integrationstests. Kennst du den Unterschied noch? Falls nicht, recherchiere das kurz.

### Teilaufgabe 1: Integrationstests

- **Erstelle** ein eigenes Projekt für "normale" Tests.
- **Erstelle** Integrationstests für die Funktionen im Bookingservice.
- **Teste** die Funktionen mit verschiedenen Parametern und prüfe, ob sie das richtige Resultat zurückgeben.

Frische dein Wissen über Testing auf, zum Beispiel mit den folgenden Quellen:  
- [xUnit - Getting Started mit .NET Core in Visual Studio](https://xunit.net/docs/getting-started/v2/getting-started)  
- [Unit Testing with dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)

Erstelle folgendes Grundgerüst:

```csharp
using System;
using Xunit.Abstractions;

namespace LedgerTest
{
    public class LedgerRepositoryTests
    {
        private readonly ITestOutputHelper output;

        public LedgerRepositoryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void GetAllLedgers_ReturnsAllLedgers()
        {
            Assert.True(true, "Test not implemented");
        }
    }    
}
```

- Mit `output` kannst du Informationen aus den Tests ausgeben.
- Die "Collection" verhindert das parallele Ausführen der Tests.
- Erstelle Unittests nach dem [FIRST-Prinzip](https://medium.com/@tasdikrahman/f-i-r-s-t-principles-of-testing-1a497acda8d6) und dem "Arrange/Act/Assert"-Prinzip, sodass du eine vernünftige Coverage in den relevanten Klassen im Backend erreichst.

Teste auch, dass Fehlerzustände nicht auftreten können, zum Beispiel:
- Es wird nicht gebucht, wenn die Buchung **grösser** ist als die zur Verfügung stehende Balance.
- Ein Ledger wird nicht geändert, wenn eine falsche ID verwendet wird.
- Edge-Cases, wie wenn der genaue Betrag auf einem Ledger gebucht wird und die Balance danach 0 ist.

Da wir für die Multiusertests die "echte" Datenbank verwenden, mocken wir die Datenbank nicht. In einer echten Applikation würden Frameworks wie "Moq" für diese Aufgabe eingesetzt. (Falls du willst darfst du die Tests auch mit Moq schreiben oder eine In-Memory-Datenbank verwenden.)

Die gesuchten Fachleute sind diejenigen, die ChatGPT für die mühsamen Arbeiten verwenden und den Output verstehen sowie korrigieren können. Um etwas zu verstehen, muss man die mühsame Arbeit normalerweise zuerst selbst erledigen. Finde also eine gute Mischung zwischen selbst erstellten Unittests und der Hilfe von ChatGPT.

> **Wichtig:** Damit du die Unittests ohne Mocks durchführen kannst, musst du Dependency Injection verwenden. Hierfür benötigst du noch ein weiteres NuGet-Package für xUnit. Installiere hierfür `Xunit.Microsoft.DependencyInjection`.

### Teilaufgabe 2: Parallele Unittests (Optional, da wir das mit den Loadtests auch testen)

Wir erstellen ein eigenes Projekt für diese Unittests, da sie einige Regeln für Unittests verletzen:

- **Fast:**  
  Sie werden nicht schnell ausgeführt. Sie sollen möglichst viele Buchungen durchführen.
  
- **Isolated:**  
  Sie sind nicht wirklich isoliert, da du überprüfen musst, ob ein Problem aufgetreten ist.
  
- **Repeatable:**  
  Durch die Parallelität wird nicht bei jedem Durchgang exakt derselbe Ablauf durchgeführt – und das ist auch nicht das Hauptziel.

**Erstelle** ein eigenes Projekt und nenne es **Bank.Concurrent.Test**.

Erstelle eine Testklasse nach folgendem Muster:

```csharp
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Bank.Concurrent.Test
{
    public class ConcurrentTests
    {
        private readonly ITestOutputHelper output;

        public ConcurrentTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestBookingParallel()
        {
            const int numberOfBookings = 1000;
            const int users = 10;

            // Implementiere hier die parallelen Buchungen
            Task[] tasks = new Task[users];
            void UserAction(int bookingsCount, decimal startingMoney)
            {
                Random random = new Random();
                for (int i = 0; i < numberOfBookings; i++)
                {
                    // Implementiere hier die parallelen Buchungen
                    // Bestimme zwei zufällige Ledgers
                }
            }

            for (int i = 0; i < users; i++)
            {
                tasks[i] = Task.Run(() => UserAction(numberOfBookings, 1000));
            }

            Task.WaitAll(tasks);
        }
    }
}
```

- Wenn beim Testen ein Fehler angezeigt wird, kannst du die obigen Hilfen verwenden oder `users` auf 1 setzen.
- Das Ausführen dieses Tests kann lange dauern (mehr als 30 Sekunden). Versuche, das Programm und die Tests so anzupassen, dass alles nach Vorgaben funktioniert.
- Wenn von Anfang an alles funktioniert, entferne die Transaktionen und schaue, ob wir sie überhaupt brauchen und ob die Unittests das nachweisen können.