using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

namespace Bank.Tests;

// Integrationstests für LedgerRepository.Delete() gegen die Test-DB bank_test (siehe docs/integrationstests.md, IT-D1 bis IT-D4).
[Collection(DatabaseCollection.Name)]
public sealed class LedgerDeleteTests(DatabaseFixture fixture) : IDisposable
{
    private readonly TestLedgers _ledgers = new(fixture.ConnectionString);
    private readonly LedgerRepository _repository = new(fixture.DatabaseSettings, NullLogger<LedgerRepository>.Instance);

    [Fact]
    public void Delete_ZeroBalanceWithoutBookings_RemovesLedger()
    {
        // Arrange
        var ledger = _ledgers.Create(0);

        // Act
        var result = _repository.Delete(ledger);

        // Assert
        Assert.Equal(DeleteLedgerResult.Success, result);
        Assert.False(Exists(ledger));
    }

    [Fact]
    public void Delete_BalanceNotZero_IsRejectedAndLedgerStays()
    {
        var ledger = _ledgers.Create(100);

        var result = _repository.Delete(ledger);

        Assert.Equal(DeleteLedgerResult.HasBalance, result);
        Assert.True(Exists(ledger));
        Assert.Equal(100, _ledgers.Balance(ledger));
    }

    [Fact]
    public void Delete_LedgerWithBookings_IsRejectedAndLedgerStays()
    {
        // Ganzen Saldo wegbuchen: Das Konto steht auf 0, hat aber einen Eintrag in der Historie.
        var ledger = _ledgers.Create(100);
        var destination = _ledgers.Create(0);
        var bookingRepository = new BookingRepository(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);
        Assert.Equal(BookingResult.Success, bookingRepository.Book(ledger, destination, 100));

        var result = _repository.Delete(ledger);

        Assert.Equal(DeleteLedgerResult.HasBookings, result);
        Assert.True(Exists(ledger));
        Assert.Equal(1, _ledgers.CountBookings());
    }

    [Fact]
    public void Delete_UnknownLedger_ReturnsNotFound()
    {
        var result = _repository.Delete(int.MaxValue);

        Assert.Equal(DeleteLedgerResult.LedgerNotFound, result);
    }

    public void Dispose() => _ledgers.Dispose();

    private bool Exists(int id)
    {
        using var conn = new MySqlConnection(fixture.ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM {Ledger.CollectionName} WHERE id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
