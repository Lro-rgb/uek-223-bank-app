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

        // Beträge unter einem Rappen ergäben Saldi wie 6932.9999999 (externer Test, F7).
        if (decimal.Round(amount, 2) != amount)
        {
            return BookingResult.TooManyDecimals;
        }

        if (sourceLedgerId == destinationLedgerId)
        {
            return BookingResult.SameLedger;
        }

        return BookingResult.Success;
    }
}
