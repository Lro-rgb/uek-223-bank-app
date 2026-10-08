namespace Bank.Core.Models;

public enum BookingResult
{
    Success,
    InvalidAmount,
    SameLedger,
    LedgerNotFound,
    InsufficientFunds,
    RetriesExhausted,
    TooManyDecimals
}
