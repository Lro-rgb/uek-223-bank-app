using Bank.DbAccess.Data;

namespace Bank.DbAccess.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public bool Book(int sourceLedgerId, int destinationLKedgerId, decimal amount)
    {
        // Machen Sie eine Connection und eine Transaktion. Noch besser ist es, das in einem Service zu machen.

        // In der Transaktion:

        // Schauen Sie ob genügend Geld beim Spender da ist
        // Führen Sie die Buchung durch und UPDATEn Sie die ledgers
        // Beenden Sie die Transaktion
        // Bei einem Transaktionsproblem: Restarten Sie die Transaktion in einer Schleife 

        return false; // Lösch mich
    }
}