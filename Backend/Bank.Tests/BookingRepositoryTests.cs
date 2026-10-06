using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.Tests;

// Integrationstests für Book() gegen die Test-DB bank_test (siehe README, Massnahmen 1, 2 und 4).
[Collection(DatabaseCollection.Name)]
public sealed class BookingRepositoryTests(DatabaseFixture fixture) : IDisposable
{
    private readonly TestLedgers _ledgers = new(fixture.ConnectionString);
    private readonly BookingRepository _repository = new(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);

    [Fact]
    public void Book_ValidAmount_MovesMoneyAndKeepsTotal()
    {
        // Arrange
        var source = _ledgers.Create(500);
        var destination = _ledgers.Create(100);

        // Act
        var result = _repository.Book(source, destination, 200);

        // Assert
        Assert.Equal(BookingResult.Success, result);
        Assert.Equal(300, _ledgers.Balance(source));
        Assert.Equal(300, _ledgers.Balance(destination));
        Assert.Equal(600, _ledgers.Total());
    }

    [Fact]
    public void Book_AmountGreaterThanBalance_IsRejectedAndNothingChanges()
    {
        var source = _ledgers.Create(50);
        var destination = _ledgers.Create(0);

        var result = _repository.Book(source, destination, 50.01m);

        Assert.Equal(BookingResult.InsufficientFunds, result);
        Assert.Equal(50, _ledgers.Balance(source));
        Assert.Equal(0, _ledgers.Balance(destination));
        Assert.Equal(0, _ledgers.CountBookings());
    }

    [Fact]
    public void Book_ExactBalance_LeavesSourceAtZero()
    {
        var source = _ledgers.Create(975);
        var destination = _ledgers.Create(25);

        var result = _repository.Book(source, destination, 975);

        Assert.Equal(BookingResult.Success, result);
        Assert.Equal(0, _ledgers.Balance(source));
        Assert.Equal(1000, _ledgers.Balance(destination));
    }

    [Fact]
    public void Book_UnknownLedger_ReturnsNotFoundAndNothingChanges()
    {
        var source = _ledgers.Create(100);

        var result = _repository.Book(source, int.MaxValue, 10);

        Assert.Equal(BookingResult.LedgerNotFound, result);
        Assert.Equal(100, _ledgers.Balance(source));
        Assert.Equal(0, _ledgers.CountBookings());
    }

    [Fact]
    public void Book_Success_CreatesExactlyOneBookingEntry()
    {
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);

        _repository.Book(source, destination, 40);

        Assert.Equal(1, _ledgers.CountBookings());
    }

    public void Dispose() => _ledgers.Dispose();
}
