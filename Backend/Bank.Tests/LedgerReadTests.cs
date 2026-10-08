using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.Tests;

// Integrationstests für das Lesen und Ändern in LedgerRepository gegen die Test-DB bank_test (siehe docs/integrationstests.md,
// IT-L1 bis IT-L4). Anlass war der externe Test (F1): Ein Konto mit name = NULL liess GET /Ledgers endlos hängen.
[Collection(DatabaseCollection.Name)]
public sealed class LedgerReadTests(DatabaseFixture fixture) : IDisposable
{
    private readonly TestLedgers _ledgers = new(fixture.ConnectionString);
    private readonly LedgerRepository _repository = new(fixture.DatabaseSettings, NullLogger<LedgerRepository>.Instance);

    [Fact]
    public void GetAllLedgers_LedgerWithoutName_ReturnsLedgerWithNullName()
    {
        // Arrange
        var ledger = _ledgers.Create(100);
        _ledgers.ClearName(ledger);

        // Act
        var all = RunWithTimeout(() => _repository.GetAllLedgers().ToList());

        // Assert
        var read = Assert.Single(all, l => l.Id == ledger);
        Assert.Null(read.Name);
        Assert.Equal(100, read.Balance);
    }

    [Fact]
    public void SelectOne_LedgerWithoutName_ReturnsLedgerWithNullName()
    {
        var ledger = _ledgers.Create(50);
        _ledgers.ClearName(ledger);

        var read = RunWithTimeout(() => _repository.SelectOne(ledger));

        Assert.NotNull(read);
        Assert.Null(read.Name);
        Assert.Equal(50, read.Balance);
    }

    [Fact]
    public void SelectOne_UnknownLedger_ReturnsNull()
    {
        var read = RunWithTimeout(() => _repository.SelectOne(int.MaxValue));

        Assert.Null(read);
    }

    [Fact]
    public void Update_UnknownLedger_ReturnsFalse()
    {
        var updated = _repository.Update(new Ledger { Id = int.MaxValue, Name = "Gibt es nicht", Balance = 1 });

        Assert.False(updated);
    }

    // Vor dem Fix lief das Lesen bei name = NULL endlos. Der Test soll dann rot werden statt hängen zu bleiben.
    private static T RunWithTimeout<T>(Func<T> read)
    {
        var task = Task.Run(read);
        Assert.True(task.Wait(TimeSpan.FromSeconds(10)), "Lesen hat nach 10 Sekunden nicht geantwortet.");
        return task.Result;
    }

    public void Dispose() => _ledgers.Dispose();
}
