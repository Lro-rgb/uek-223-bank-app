namespace Bank.Core.Models;

public enum DeleteLedgerResult
{
    Success,
    LedgerNotFound,
    HasBalance,
    HasBookings,
    RetriesExhausted
}
