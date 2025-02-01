using Bank.Core.Models;
using Bank.DbAccess.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Bank.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BookingsController(IBookingRepository bookingRepository) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrators")]
    public async Task<IActionResult> Post([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Booking booking)
    {
        return await Task.Run(() =>
        {
            IActionResult response = Ok();

            // Rufe "Book" im "BookingRepository" auf.
            // Noch besser wäre es, wenn du einen Service verwenden würdest, der die Geschäftslogik enthält.
            // Gib je nach Erfolg OK() oder Conflict() zurück
            return response;
        });
    }
}