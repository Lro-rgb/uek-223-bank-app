using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Bank.Web.Controllers;
using Bank.Web.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.Tests;

// Integrationstests für das Lesen und Ändern in LedgerRepository gegen die Test-DB bank_test (siehe docs/integrationstests.md,
// IT-L1 bis IT-L6). Anlass war der externe Test (F1): Ein Konto mit name = NULL liess GET /Ledgers endlos hängen.
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
    public void UpdateName_UnknownLedger_ReturnsFalse()
    {
        var updated = _repository.UpdateName(int.MaxValue, "Gibt es nicht");

        Assert.False(updated);
    }

    // Lost Update über PUT: Ein Admin hat das Konto mit Saldo 100 geladen, dazwischen wird 30 gebucht, dann speichert er
    // den neuen Namen. Früher schrieb PUT den alten Saldo 100 zurück und die Buchung ging verloren. UpdateName() ändert
    // den Saldo nicht, die Buchung bleibt erhalten.
    [Fact]
    public void UpdateName_AfterParallelBooking_KeepsBookedBalance()
    {
        // Arrange
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);
        var loadedByAdmin = _repository.SelectOne(source);
        var bookings = new BookingRepository(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);
        Assert.Equal(BookingResult.Success, bookings.Book(source, destination, 30));

        // Act
        var updated = _repository.UpdateName(source, "Umbenannt");

        // Assert
        Assert.True(updated);
        Assert.Equal(100, loadedByAdmin!.Balance);
        var after = _repository.SelectOne(source);
        Assert.Equal("Umbenannt", after!.Name);
        Assert.Equal(70, after.Balance);
        Assert.Equal(100, _ledgers.Total());
    }

    // Derselbe Ablauf über den Controller: Der Admin schickt den alten Saldo 100 mit, nach der Buchung ist der aktuelle
    // Saldo aber 70. PUT lehnt mit 400 ab und ändert weder Name noch Saldo. Ohne Saldo im Body wird nur umbenannt.
    [Fact]
    public void Put_WithOutdatedBalance_IsRejectedAndNothingChanges()
    {
        // Arrange
        var source = _ledgers.Create(100);
        var destination = _ledgers.Create(0);
        var loadedByAdmin = _repository.SelectOne(source)!;
        var bookings = new BookingRepository(fixture.DatabaseSettings, NullLogger<BookingRepository>.Instance);
        Assert.Equal(BookingResult.Success, bookings.Book(source, destination, 30));
        var controller = new LedgersController(_repository);

        // Act
        var rejected = controller.Put(source, new LedgerUpdateDto { Id = source, Name = "Umbenannt", Balance = loadedByAdmin.Balance });
        var afterRejected = _repository.SelectOne(source)!;
        var renamed = controller.Put(source, new LedgerUpdateDto { Name = "Umbenannt" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(rejected);
        Assert.Equal(loadedByAdmin.Name, afterRejected.Name);
        Assert.Equal(70, afterRejected.Balance);
        Assert.IsType<OkResult>(renamed);
        var after = _repository.SelectOne(source)!;
        Assert.Equal("Umbenannt", after.Name);
        Assert.Equal(70, after.Balance);
        Assert.Equal(100, _ledgers.Total());
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
