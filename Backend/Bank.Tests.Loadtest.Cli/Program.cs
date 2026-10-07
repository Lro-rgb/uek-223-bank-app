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

    // Welches Szenario läuft: "bookings" (Teilaufgabe 4) oder "bankinfo" (Teilaufgabe 2). Überschreibbar mit dotnet run -- bankinfo
    private const string DefaultScenario = "bookings";

    // Last für das Buchungs-Szenario
    private const int BookingRate = 50;
    private static readonly TimeSpan BookingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BookingDuration = TimeSpan.FromSeconds(30);

    // Betrag pro Buchung (ganze Franken). Gross genug, dass Konten leer werden und 409 (zu wenig Saldo) vorkommt.
    private const int MinAmount = 1;
    private const int MaxAmount = 500;

    private const string BookingReportFolder = "booking_reports";

    private static readonly HttpClient HttpClient = new();

    // Statuscodes des Buchungs-Szenarios, werden parallel hochgezählt
    private static int _status200;
    private static int _status409;
    private static int _status400;
    private static int _status404;
    private static int _status5xx;
    private static int _statusOther;

    public static async Task<int> Main(string[] args)
    {
        var scenarioName = args.Length > 0 ? args[0] : DefaultScenario;
        if (scenarioName != "bookings" && scenarioName != "bankinfo")
        {
            Console.WriteLine($"Unbekanntes Szenario '{scenarioName}'. Erlaubt: bookings, bankinfo");
            return 2;
        }

        var jwt = await Login(Username, Password);
        Console.WriteLine("Login ok.");

        var ledgersBefore = (await GetAllLedgers(jwt)).ToList();
        foreach (var ledger in ledgersBefore)
        {
            Console.WriteLine($"{ledger.Name}: {ledger.Balance:F2}");
        }

        var totalBefore = ledgersBefore.Sum(l => l.Balance);

        var runner = scenarioName == "bookings"
            ? NBomberRunner
                .RegisterScenarios(CreateBookingScenario(jwt, ledgersBefore))
                .WithReportFileName("booking_report")
                .WithReportFolder(BookingReportFolder)
            : NBomberRunner
                .RegisterScenarios(CreateBankInfoScenario(jwt))
                .WithReportFileName("fetch_users_report")
                .WithReportFolder("fetch_users_reports");

        var stats = runner
            .WithReportFormats(ReportFormat.Html)
            .Run();

        var ledgersAfter = (await GetAllLedgers(jwt)).ToList();
        var totalAfter = ledgersAfter.Sum(l => l.Balance);
        var negativeLedgers = ledgersAfter.Where(l => l.Balance < 0).ToList();

        var scenarioStats = stats.ScenarioStats.First();
        Console.WriteLine();
        Console.WriteLine($"Szenario: {scenarioName}");
        Console.WriteLine($"Anfragen total: {scenarioStats.Ok.Request.Count + scenarioStats.Fail.Request.Count}");
        Console.WriteLine($"Anfragen ok / fehlgeschlagen: {scenarioStats.Ok.Request.Count} / {scenarioStats.Fail.Request.Count}");
        Console.WriteLine($"Durchschnittsdauer (ok): {scenarioStats.Ok.Latency.MeanMs:F2} ms");
        if (scenarioName == "bookings")
        {
            Console.WriteLine($"Statuscodes: 200: {_status200}, 409: {_status409}, 400: {_status400}, 404: {_status404}, 5xx: {_status5xx}, andere: {_statusOther}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total vorher:  {totalBefore:F2}");
        Console.WriteLine($"Total nachher: {totalAfter:F2}");
        foreach (var ledger in ledgersAfter)
        {
            Console.WriteLine($"  {ledger.Name}: {ledger.Balance:F2}");
        }

        Console.WriteLine();
        var passed = true;
        passed &= Check("a) Total vorher = Total nachher", totalBefore == totalAfter);
        passed &= Check("b) kein Konto unter 0", negativeLedgers.Count == 0,
            string.Join(", ", negativeLedgers.Select(l => $"{l.Name}: {l.Balance:F2}")));
        if (scenarioName == "bookings")
        {
            passed &= Check("c) keine 5xx", _status5xx == 0 && _statusOther == 0,
                $"5xx: {_status5xx}, andere: {_statusOther}");
            // TODO: Sobald GET /api/v1/Bookings?ledgerId= existiert (Historie, Zusatz-Feature 2), die Einträge aller
            // Konten vor und nach dem Lauf zählen und mit _status200 vergleichen (Beleg Massnahme 4).
            Console.WriteLine($"TODO d) Anzahl 200er ({_status200}) = Anzahl neue Einträge in Bookings: GET /api/v1/Bookings?ledgerId= fehlt noch");
        }

        Console.WriteLine();
        Console.WriteLine(passed ? "Ergebnis: PASS" : "Ergebnis: FAIL");
        return passed ? 0 : 1;
    }

    private static bool Check(string name, bool ok, string details = "")
    {
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}{(ok || details == "" ? "" : $" ({details})")}");
        return ok;
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

    // Teilaufgabe 4: Lasttest auf POST /bookings. Jede Anfrage bucht einen zufälligen Betrag von einem zufälligen
    // Konto auf ein anderes. 409 (zu wenig Saldo oder Retries aufgebraucht) ist ein erwartetes Ergebnis,
    // 5xx ist ein Fehler im System.
    private static ScenarioProps CreateBookingScenario(string jwt, IReadOnlyList<LedgerDto> ledgers)
    {
        if (ledgers.Count < 2)
        {
            throw new InvalidOperationException("Für den Buchungs-Lasttest braucht es mindestens 2 Konten.");
        }

        return Scenario.Create("bookings", async _ =>
            {
                var sourceIndex = Random.Shared.Next(ledgers.Count);
                // Ziel ist nie gleich Quelle: einen Index aus den übrigen Konten wählen
                var destinationIndex = (sourceIndex + Random.Shared.Next(1, ledgers.Count)) % ledgers.Count;
                var payload = new
                {
                    sourceId = ledgers[sourceIndex].Id,
                    destinationId = ledgers[destinationIndex].Id,
                    amount = (decimal)Random.Shared.Next(MinAmount, MaxAmount + 1)
                };

                var request = Http.CreateRequest("POST", $"{BaseUrl}/bookings")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Authorization", $"Bearer {jwt}")
                    .WithBody(JsonContent.Create(payload));

                var response = await Http.Send(HttpClient, request);
                var statusCode = response.Payload.IsSome() ? (int)response.Payload.Value.StatusCode : 0;

                switch (statusCode)
                {
                    case 200:
                        Interlocked.Increment(ref _status200);
                        return Response.Ok(statusCode: "200");
                    case 409:
                        Interlocked.Increment(ref _status409);
                        return Response.Ok(statusCode: "409");
                    case 400:
                        Interlocked.Increment(ref _status400);
                        return Response.Fail(statusCode: "400", message: "Ungültige Buchung");
                    case 404:
                        Interlocked.Increment(ref _status404);
                        return Response.Fail(statusCode: "404", message: "Konto nicht gefunden");
                    case >= 500:
                        Interlocked.Increment(ref _status5xx);
                        return Response.Fail(statusCode: statusCode.ToString(), message: "Serverfehler");
                    default:
                        // z.B. Timeout oder Verbindungsfehler (kein HTTP-Status)
                        Interlocked.Increment(ref _statusOther);
                        return Response.Fail(statusCode: response.StatusCode, message: response.Message);
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: BookingRate, interval: BookingInterval, during: BookingDuration)
            );
    }
}
