using System.Collections.Concurrent;
using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Bank.Tests;

// Nachweis der Transaktionssicherheit (Kriterium 6b, README Massnahmen 1–3).
[Collection(DatabaseCollection.Name)]
public sealed class BookingConcurrencyTests(DatabaseFixture fixture, ITestOutputHelper output) : IDisposable
{
    private readonly TestLedgers _ledgers = new(fixture.ConnectionString);
    private readonly BookingRepository _repository = new(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);

    // Deterministischer Test: Zwei Buchungen über 80 von einem Konto mit 100. Die erste wird nach ihrer Saldo-Prüfung
    // angehalten, genau dann startet die zweite. Mit FOR UPDATE wartet die zweite in der DB, bis die erste committet,
    // sieht dann 20 und wird abgelehnt. Ohne Sperre liest sie noch 100, bucht ebenfalls, und der Saldo wird -60.
    [Fact]
    public void Book_TwoBookingsAtSameTime_SecondSeesNewBalanceAndIsRejected()
    {
        // Arrange
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);
        using var firstChecked = new ManualResetEventSlim();
        using var secondChecked = new ManualResetEventSlim();
        var calls = 0;
        _repository.AfterBalanceCheck = () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                firstChecked.Set();
                // Erste Buchung festhalten. Kommt die zweite auch durch die Prüfung, geht es sofort weiter,
                // sonst (sie wartet auf die Sperre) nach spätestens 2 Sekunden.
                secondChecked.Wait(TimeSpan.FromSeconds(2));
            }
            else
            {
                secondChecked.Set();
            }
        };

        // Act
        var first = Task.Run(() => _repository.Book(source, destination, 80));
        Assert.True(firstChecked.Wait(TimeSpan.FromSeconds(10)), "Erste Buchung hat die Saldo-Prüfung nicht erreicht.");
        var second = Task.Run(() => _repository.Book(source, destination, 80));
        Task.WaitAll(first, second);

        // Assert
        Assert.Equal(BookingResult.Success, first.Result);
        Assert.Equal(BookingResult.InsufficientFunds, second.Result);
        Assert.Equal(20, _ledgers.Balance(source));
        Assert.Equal(80, _ledgers.Balance(destination));
        Assert.Equal(1, _ledgers.CountBookings());
    }

    // Lasttest: Viele parallele Buchungen zwischen 2 Konten mit wenig Geld. Prüft, dass unter Last das Total stimmt,
    // kein Saldo unter 0 fällt und jede erfolgreiche Buchung genau einen Historien-Eintrag hat.
    [Fact]
    public void Book_ParallelBookings_KeepTotalAndNoNegativeBalance()
    {
        // Arrange
        const int users = 20;
        const int bookingsPerUser = 100;
        var ids = new[] { _ledgers.Create(100), _ledgers.Create(100) };
        var totalBefore = _ledgers.Total();
        var results = new ConcurrentBag<BookingResult>();

        // Act
        Parallel.For(0, users, new ParallelOptions { MaxDegreeOfParallelism = users }, _ =>
        {
            for (var i = 0; i < bookingsPerUser; i++)
            {
                var sourceIndex = Random.Shared.Next(2);
                results.Add(_repository.Book(ids[sourceIndex], ids[1 - sourceIndex], Random.Shared.Next(50, 101)));
            }
        });

        // Assert
        var successes = results.Count(r => r == BookingResult.Success);
        output.WriteLine($"Buchungen: {results.Count}, erfolgreich: {successes}, " +
                         $"zu wenig Guthaben: {results.Count(r => r == BookingResult.InsufficientFunds)}, " +
                         $"Retries aufgebraucht: {results.Count(r => r == BookingResult.RetriesExhausted)}");
        output.WriteLine($"Total vorher: {totalBefore}, nachher: {_ledgers.Total()}");

        Assert.Equal(totalBefore, _ledgers.Total());
        Assert.All(ids, id => Assert.True(_ledgers.Balance(id) >= 0, $"Konto {id} ist unter 0."));
        Assert.Equal(successes, _ledgers.CountBookings());
        Assert.True(successes > 0);
    }

    public void Dispose() => _ledgers.Dispose();
}
