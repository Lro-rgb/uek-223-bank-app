using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.Tests;

// Integrationstests für die Buchungshistorie (Zusatz-Feature 2, Issue #10, Anforderung A6).
[Collection(DatabaseCollection.Name)]
public sealed class BookingHistoryTests(DatabaseFixture fixture) : IDisposable
{
    private readonly TestLedgers _ledgers = new(fixture.ConnectionString);
    private readonly BookingRepository _repository = new(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);

    [Fact]
    public void GetHistory_SuccessfulBooking_AppearsExactlyOnceForBothLedgers()
    {
        // Arrange
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);

        // Act
        _repository.Book(source, destination, 30);
        var sourceHistory = _repository.GetHistory(source);
        var destinationHistory = _repository.GetHistory(destination);

        // Assert
        var entry = Assert.Single(sourceHistory!);
        Assert.Equal(source, entry.SourceId);
        Assert.Equal(destination, entry.DestinationId);
        Assert.Equal(30, entry.Amount);
        Assert.Equal(entry.Id, Assert.Single(destinationHistory!).Id);
    }

    [Fact]
    public void GetHistory_RejectedBooking_DoesNotAppear()
    {
        var source = _ledgers.Create(50);
        var destination = _ledgers.Create(0);

        var result = _repository.Book(source, destination, 51);

        Assert.Equal(BookingResult.InsufficientFunds, result);
        Assert.Empty(_repository.GetHistory(source)!);
        Assert.Empty(_repository.GetHistory(destination)!);
    }

    [Fact]
    public void GetHistory_UnknownLedger_ReturnsNull()
    {
        Assert.Null(_repository.GetHistory(int.MaxValue));
    }

    [Fact]
    public void GetHistory_SeveralBookings_NewestFirst()
    {
        var first = _ledgers.Create(100);
        var second = _ledgers.Create(100);

        _repository.Book(first, second, 10);
        _repository.Book(second, first, 20);
        var history = _repository.GetHistory(first)!;

        Assert.Equal(2, history.Count);
        Assert.Equal(20, history[0].Amount);
        Assert.Equal(10, history[1].Amount);
    }

    [Fact]
    public void GetHistory_LedgerWithoutName_ReturnsEntryWithNullName()
    {
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);
        _repository.Book(source, destination, 10);
        _ledgers.ClearName(source);

        var entry = Assert.Single(_repository.GetHistory(destination)!);

        Assert.Null(entry.SourceName);
        Assert.NotNull(entry.DestinationName);
    }

    public void Dispose() => _ledgers.Dispose();
}
