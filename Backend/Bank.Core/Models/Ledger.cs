namespace Bank.Core.Models;

public class Ledger
{
    public const string CollectionName = "Ledgers";
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Balance { get; set; }
}