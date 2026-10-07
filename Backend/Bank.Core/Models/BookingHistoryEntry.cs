namespace Bank.Core.Models;

public class BookingHistoryEntry
{
    public int Id { get; init; }
    public int SourceId { get; init; }
    public string? SourceName { get; init; }
    public int DestinationId { get; init; }
    public string? DestinationName { get; init; }
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; }
}
