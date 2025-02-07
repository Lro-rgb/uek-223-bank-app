# EF Core (Optional)

EF (EntityFramework Core) ist ein Object-Relational-Mapper (ORM) für .NET. Es ermöglicht die Interaktion mit einer Datenbank, ohne SQL schreiben zu müssen. EF Core ist eine leichtgewichtige, modulare Version von EF, die in .NET Core Anwendungen verwendet wird.

Deine Applikation verwendet derzeit an gewissen Orten EF Core. Prüfe, wo EF Core verwendet wird und wo noch nicht.

Baue die bestehende Applikation so um, dass sie EF Core verwendet.

[Erste Schritte mit EF Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli)

### Teilaufgabe 1: Anfragen anpassen

Versuche anhand dieses Tutorials die Anfragen in der Applikation so umzubauen, dass sie EF Core verwenden. -> [EF Core Querying](https://learn.microsoft.com/en-us/ef/core/querying/)

Wichtig: Bisher wurden die Balances neu geladen durch die Methode `Ledger.GetBalance()`. EF kann seine Objekte jederzeit neu laden.  
`context.Entry(from).Reload()`

### Teilaufgabe 2: Transaktionen

Zuletzt muss auch hier eine Transaktion eingebaut werden: [EF Core Transaktionen](https://learn.microsoft.com/en-us/ef/core/saving/transactions)

Wichtig zum Einbau der Transaktion: Damit ein Isolationslevel (Serializable, ReadCommitted, …) angegeben werden kann, braucht es das NuGet-Package Microsoft.EntityFrameworkCore.Relational. Füge dies deinem Projekt hinzu. Entweder über das Kontextmenü bei Abhängigkeiten oder via Konsole mit dem Befehl:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Relational
```