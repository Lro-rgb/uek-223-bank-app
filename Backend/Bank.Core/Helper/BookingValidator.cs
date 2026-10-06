using Bank.Core.Models;

namespace Bank.Core.Helper;

public static class BookingValidator
{
    // Prüft nur die Eingaben, ohne Datenbank. Saldo und Existenz der Konten prüft BookingRepository.Book().
    public static BookingResult Validate(int sourceLedgerId, int destinationLedgerId, decimal amount)
    {
        if (amount <= 0)
        {
            return BookingResult.InvalidAmount;
        }

        if (sourceLedgerId == destinationLedgerId)
        {
            return BookingResult.SameLedger;
        }

        return BookingResult.Success;
    }
}
