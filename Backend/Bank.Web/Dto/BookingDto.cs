namespace Bank.Web.Dto;

public class BookingDto
{
    public int SourceId { get; set; }
    public int DestinationId { get; set; }
    public decimal Amount { get; set; }
}
