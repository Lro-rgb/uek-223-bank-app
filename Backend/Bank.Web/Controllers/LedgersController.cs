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
    public IActionResult Get(int id)
    {
        // Vorher wurde aus null ein 204 ohne Inhalt (externer Test, F5).
        var ledger = ledgerRepository.SelectOne(id);
        return ledger is null
            ? NotFound(new { message = "Konto nicht gefunden." })
            : Ok(ledger);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrators")]
    public IActionResult Put(int id, [FromBody] Ledger ledger)
    {
        // Es gilt die ID aus der URL. Vorher wurde die ID aus dem Body verwendet (externer Test, F2).
        if (ledger.Id != 0 && ledger.Id != id)
        {
            return BadRequest(new { message = "ID in URL und Body stimmen nicht überein." });
        }
        // Ohne Name würde name = NULL gespeichert, z.B. bei einem Body nur mit balance (externer Test, F1).
        if (string.IsNullOrWhiteSpace(ledger.Name))
        {
            return BadRequest(new { message = "Name fehlt." });
        }

        ledger.Id = id;
        return ledgerRepository.Update(ledger)
            ? Ok()
            : NotFound(new { message = "Konto nicht gefunden." });
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