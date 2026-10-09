namespace Bank.Web.Dto;

// Body für PUT /api/v1/Ledgers/{id}. Geändert wird nur der Name, der Saldo nur über Buchungen.
// Id und Balance sind optional, damit bestehende Clients das ganze Konto mitschicken können.
public class LedgerUpdateDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public decimal? Balance { get; set; }
}
