using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Bank.Web.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BookingsController(IBookingRepository bookingRepository) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrators,Users")]
    public IActionResult Get([FromQuery] string? ledgerId)
    {
        // Als string entgegennehmen und selbst parsen: Bei "abc" würde [ApiController] sonst eine ProblemDetails-Antwort
        // ohne { message } liefern, die das Frontend nicht anzeigen kann.
        if (string.IsNullOrWhiteSpace(ledgerId))
        {
            return BadRequest(new { message = "Parameter ledgerId fehlt." });
        }
        if (!int.TryParse(ledgerId, out var id))
        {
            return BadRequest(new { message = "Parameter ledgerId muss eine Zahl sein." });
        }

        var history = bookingRepository.GetHistory(id);
        return history is null
            ? NotFound(new { message = "Konto nicht gefunden." })
            : Ok(history);
    }

    [HttpPost]
    [Authorize(Roles = "Administrators,Users")]
    public IActionResult Post([FromBody] BookingDto booking)
    {
        var result = bookingRepository.Book(booking.SourceId, booking.DestinationId, booking.Amount);

        // Fehlermeldungen immer als { "message": "..." }, das Frontend zeigt error.error.message an (API-Vertrag in #6).
        return result switch
        {
            BookingResult.Success => Ok(),
            BookingResult.InvalidAmount => BadRequest(new { message = "Der Betrag muss grösser als 0 sein." }),
            BookingResult.TooManyDecimals => BadRequest(new { message = "Der Betrag darf höchstens 2 Nachkommastellen haben." }),
            BookingResult.SameLedger =>BadRequest(new { message = "Quelle und Ziel müssen verschiedene Konten sein." }),
            BookingResult.LedgerNotFound => NotFound(new { message = "Konto nicht gefunden." }),
            BookingResult.InsufficientFunds => Conflict(new { message = "Zu wenig Guthaben auf dem Quellkonto." }),
            BookingResult.RetriesExhausted => Conflict(new { message = "Die Buchung ist wegen gleichzeitiger Zugriffe fehlgeschlagen. Bitte erneut versuchen." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unbekannter Fehler bei der Buchung." })
        };
    }
}
