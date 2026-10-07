using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class LedgersController(ILedgerRepository ledgerRepository) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrators,Users")]
    public IEnumerable<Ledger> Get()
    {
        var allLedgers = ledgerRepository.GetAllLedgers();
        return allLedgers;
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrators,Users")]
    public Ledger? Get(int id)
    {
        var ledger = ledgerRepository.SelectOne(id);
        return ledger;
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrators")]
    public void Put(int id, [FromBody] Ledger ledger)
    {
        ledgerRepository.Update(ledger);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrators")]
    public IActionResult Delete(int id)
    {
        var result = ledgerRepository.Delete(id);

        // Fehlermeldungen wie beim Buchen als { "message": "..." }, das Frontend zeigt error.error.message an.
        return result switch
        {
            DeleteLedgerResult.Success => NoContent(),
            DeleteLedgerResult.LedgerNotFound => NotFound(new { message = "Konto nicht gefunden." }),
            DeleteLedgerResult.HasBalance => Conflict(new { message = "Konto hat noch Guthaben." }),
            DeleteLedgerResult.HasBookings => Conflict(new { message = "Konto hat Buchungen." }),
            DeleteLedgerResult.RetriesExhausted => Conflict(new { message = "Das Konto konnte wegen gleichzeitiger Zugriffe nicht gelöscht werden. Bitte erneut versuchen." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unbekannter Fehler beim Löschen." })
        };
    }
}