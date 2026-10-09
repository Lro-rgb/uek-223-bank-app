using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Bank.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Bank.Tests;

// Reine Unit-Tests ohne Datenbank für Konto löschen (Zusatz-Feature 1): Der Controller wird mit einem Fake-Repository
// geprüft, das ein festes Ergebnis zurückgibt. Getestet wird nur die Umsetzung in HTTP-Status und Meldung (siehe
// docs/integrationstests.md, UT-C1 bis UT-C3). Die Transaktion selbst prüfen IT-D1 bis IT-D4 gegen bank_test.
public class LedgersControllerTests
{
    [Fact]
    public void Delete_Success_ReturnsNoContent()
    {
        var repository = new FakeLedgerRepository(DeleteLedgerResult.Success);
        var controller = new LedgersController(repository);

        var result = controller.Delete(4);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(4, repository.DeletedId);
    }

    [Fact]
    public void Delete_LedgerNotFound_ReturnsNotFoundWithMessage()
    {
        var controller = new LedgersController(new FakeLedgerRepository(DeleteLedgerResult.LedgerNotFound));

        var result = controller.Delete(int.MaxValue);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Konto nicht gefunden.", MessageOf(notFound));
    }

    [Theory]
    [InlineData(DeleteLedgerResult.HasBalance, "Konto hat noch Guthaben.")]
    [InlineData(DeleteLedgerResult.HasBookings, "Konto hat Buchungen.")]
    [InlineData(DeleteLedgerResult.RetriesExhausted, "Das Konto konnte wegen gleichzeitiger Zugriffe nicht gelöscht werden. Bitte erneut versuchen.")]
    public void Delete_Rejected_ReturnsConflictWithMessage(DeleteLedgerResult deleteResult, string message)
    {
        var controller = new LedgersController(new FakeLedgerRepository(deleteResult));

        var result = controller.Delete(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(message, MessageOf(conflict));
    }

    // Die API gibt Fehler immer als { message } zurück (anonymes Objekt), das Frontend zeigt error.error.message an.
    private static string? MessageOf(ObjectResult result) =>
        result.Value?.GetType().GetProperty("message")?.GetValue(result.Value) as string;

    // Nur Delete() wird gebraucht, alle anderen Methoden dürfen in diesen Tests nicht aufgerufen werden.
    private sealed class FakeLedgerRepository(DeleteLedgerResult deleteResult) : ILedgerRepository
    {
        public int? DeletedId { get; private set; }

        public DeleteLedgerResult Delete(int id)
        {
            DeletedId = id;
            return deleteResult;
        }

        public IEnumerable<Ledger> GetAllLedgers() => throw new NotSupportedException();
        public void Book(decimal amount, Ledger from, Ledger to) => throw new NotSupportedException();
        public decimal GetTotalMoney() => throw new NotSupportedException();
        public Ledger? SelectOne(int id) => throw new NotSupportedException();
        public Ledger? SelectOne(int id, MySqlConnection conn, MySqlTransaction? transaction) => throw new NotSupportedException();
        public void Update(Ledger ledger, MySqlConnection conn, MySqlTransaction transaction) => throw new NotSupportedException();
        public bool Update(Ledger ledger) => throw new NotSupportedException();
        public bool UpdateName(int id, string name) => throw new NotSupportedException();
        public decimal? GetBalance(int ledgerId, MySqlConnection conn, MySqlTransaction transaction) => throw new NotSupportedException();
    }
}
