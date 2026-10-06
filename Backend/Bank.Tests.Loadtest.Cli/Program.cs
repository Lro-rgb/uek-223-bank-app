using System.Net.Http.Headers;
using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Bank.Tests.Loadtest.Cli;

public record LedgerDto(int Id, string? Name, decimal Balance);

public record LoginResponse(string Token);

public static class Program
{
    private const string BaseUrl = "http://localhost:5000/api/v1";

    // Testuser aus Bank.DbAccess/DatabaseSeeder.cs
    private const string Username = "admin";
    private const string Password = "adminpass";

    private static readonly HttpClient HttpClient = new();

    public static async Task Main()
    {
        var jwt = await Login(Username, Password);
        Console.WriteLine("Login ok.");

        var ledgersBefore = (await GetAllLedgers(jwt)).ToList();
        foreach (var ledger in ledgersBefore)
        {
            Console.WriteLine($"{ledger.Name}: {ledger.Balance:F2}");
        }

        var totalBefore = ledgersBefore.Sum(l => l.Balance);

        var stats = NBomberRunner
            .RegisterScenarios(CreateBankInfoScenario(jwt))
            .WithReportFileName("fetch_users_report")
            .WithReportFolder("fetch_users_reports")
            .WithReportFormats(ReportFormat.Html)
            .Run();

        var ledgersAfter = (await GetAllLedgers(jwt)).ToList();
        var totalAfter = ledgersAfter.Sum(l => l.Balance);
        var negativeLedgers = ledgersAfter.Where(l => l.Balance < 0).ToList();

        var scenarioStats = stats.ScenarioStats.First();
        Console.WriteLine();
        Console.WriteLine($"Anfragen ok / fehlgeschlagen: {scenarioStats.Ok.Request.Count} / {scenarioStats.Fail.Request.Count}");
        Console.WriteLine($"Durchschnittsdauer: {scenarioStats.Ok.Latency.MeanMs:F2} ms");
        Console.WriteLine();
        Console.WriteLine($"Total vorher:  {totalBefore:F2}");
        Console.WriteLine($"Total nachher: {totalAfter:F2}");
        Console.WriteLine($"Total unverändert: {(totalBefore == totalAfter ? "ja" : "nein")}");
        Console.WriteLine(negativeLedgers.Count == 0
            ? "Konto unter 0: nein"
            : $"Konto unter 0: ja ({string.Join(", ", negativeLedgers.Select(l => $"{l.Name}: {l.Balance}"))})");
    }

    private static async Task<string> Login(string username, string password)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/login", new { username, password });
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Backend nicht erreichbar ({BaseUrl}). Läuft Bank.Web?", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Login für {username} fehlgeschlagen: {(int)response.StatusCode}");
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login?.Token ?? throw new InvalidOperationException("Login-Antwort enthält kein Token.");
    }

    private static async Task<IEnumerable<LedgerDto>> GetAllLedgers(string jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/ledgers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<LedgerDto>>() ?? [];
    }

    // Teilaufgabe 2: Lasttest auf /bankinfo, 100 Anfragen pro Sekunde während 30 Sekunden
    private static ScenarioProps CreateBankInfoScenario(string jwt)
    {
        return Scenario.Create("bankinfo", async _ =>
            {
                var request = Http.CreateRequest("GET", $"{BaseUrl}/bankinfo")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {jwt}");

                return await Http.Send(HttpClient, request);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );
    }

    // Teilaufgabe 4: wird nach dem Merge von "Buchen" gefüllt.
    // - Pro Aufruf zwei zufällige Ledgers und einen Betrag wählen und POST /bookings senden
    //   (Body und Statuscodes so wie im BookingsController umgesetzt).
    // - 200er und 409er (Conflict) zählen.
    // - Beleg Massnahme 4: Anzahl 200er = Anzahl neue Einträge in Bookings
    //   (Quelle: Historien-API oder direkt aus der DB, sobald die Historie steht).
    // - Danach wie bei bankinfo Total vorher/nachher und Konto unter 0 prüfen.
    private static ScenarioProps CreateBookingScenario(string jwt, IReadOnlyList<LedgerDto> ledgers)
    {
        throw new NotImplementedException("Wird nach dem Merge von Buchen umgesetzt.");
    }
}
