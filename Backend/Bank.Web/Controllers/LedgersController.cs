using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Bank.Web.Dto;
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
    public IActionResult Put(int id, [FromBody] LedgerUpdateDto dto)
    {
        // Es gilt die ID aus der URL. Vorher wurde die ID aus dem Body verwendet (externer Test, F2).
        if (dto.Id is not null and not 0 && dto.Id != id)
        {
            return BadRequest(new { message = "ID in URL und Body stimmen nicht überein." });
        }
        // Ohne Name würde name = NULL gespeichert, z.B. bei einem Body nur mit balance (externer Test, F1).
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Name fehlt." });
        }

        // Der Saldo wird nur über Buchungen geändert. Ein absolut geschriebener Saldo würde eine gleichzeitige Buchung
        // überschreiben (Lost Update, README Probleme). Ein mitgeschickter Saldo muss deshalb dem aktuellen entsprechen.
        if (dto.Balance is not null)
        {
            var current = ledgerRepository.SelectOne(id);
            if (current is null)
            {
                return NotFound(new { message = "Konto nicht gefunden." });
            }
            if (current.Balance != dto.Balance)
            {
                return BadRequest(new { message = "Der Saldo kann nur über Buchungen geändert werden. Saldo weglassen oder den aktuellen Saldo mitschicken." });
            }
        }

        return ledgerRepository.UpdateName(id, dto.Name)
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