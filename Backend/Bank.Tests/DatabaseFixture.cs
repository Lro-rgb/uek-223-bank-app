using System.Text.Json;
using Bank.DbAccess;
using Bank.DbAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Bank.Tests;

// Alle Tests, die die Datenbank brauchen, laufen in dieser Collection. xUnit führt sie nacheinander aus,
// so stören sich z.B. die Total-Prüfungen nicht gegenseitig.
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}

public class DatabaseFixture
{
    private const string TestDatabase = "bank_test";
    private const string EnvironmentVariable = "BANK_TEST_CONNECTION_STRING";

    public string ConnectionString { get; }

    public IOptions<DatabaseSettings> DatabaseSettings => Options.Create(new DatabaseSettings { ConnectionString = ConnectionString });

    public DatabaseFixture()
    {
        ConnectionString = ResolveConnectionString();

        // Sicherheitsnetz: Die Tests legen Konten an und löschen sie wieder. Sie dürfen nie auf der echten DB "bank" laufen.
        var database = new MySqlConnectionStringBuilder(ConnectionString).Database;
        if (database != TestDatabase)
        {
            throw new InvalidOperationException($"Tests laufen nur gegen '{TestDatabase}', nicht gegen '{database}'.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(ConnectionString, new MySqlServerVersion(new Version(11, 6, 2)))
            .Options;
        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    // Reihenfolge: Umgebungsvariable BANK_TEST_CONNECTION_STRING, sonst der ConnectionString aus
    // Bank.Web/appsettings.json mit der Datenbank bank_test. So landet kein Passwort im Repo.
    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var appSettings = FindWebAppSettings()
                          ?? throw new InvalidOperationException(
                              $"Weder {EnvironmentVariable} gesetzt noch Backend/Bank.Web/appsettings.json gefunden.");

        using var json = JsonDocument.Parse(File.ReadAllText(appSettings));
        var webConnectionString = json.RootElement.GetProperty("DatabaseSettings").GetProperty("ConnectionString").GetString()
                                  ?? throw new InvalidOperationException("DatabaseSettings:ConnectionString fehlt in Bank.Web/appsettings.json.");
        return new MySqlConnectionStringBuilder(webConnectionString) { Database = TestDatabase }.ConnectionString;
    }

    private static string? FindWebAppSettings()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Bank.Web", "appsettings.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
