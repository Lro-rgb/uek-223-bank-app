using Bank.Core.Models;

namespace Bank.DbAccess.Repositories;

public interface IBookingRepository
{
    BookingResult Book(int sourceLedgerId, int destinationLedgerId, decimal amount);
}
