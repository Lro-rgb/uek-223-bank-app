using Bank.Core.Models;
using MySqlConnector;

namespace Bank.Tests;

// Legt für einen Test eigene Konten an und räumt sie (inkl. Buchungen) beim Dispose wieder auf.
public sealed class TestLedgers(string connectionString) : IDisposable
{
    private readonly List<int> _ids = [];

    public IReadOnlyList<int> Ids => _ids;

    public int Create(decimal balance)
    {
        using var conn = Open();
        using var cmd = new MySqlCommand(
            $"INSERT INTO {Ledger.CollectionName} (name, balance) VALUES (@Name, @Balance); SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@Name", $"Test {Guid.NewGuid():N}");
        cmd.Parameters.AddWithValue("@Balance", balance);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        _ids.Add(id);
        return id;
    }

    public void ClearName(int id)
    {
        using var conn = Open();
        using var cmd = new MySqlCommand($"UPDATE {Ledger.CollectionName} SET name = NULL WHERE id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public decimal Balance(int id)
    {
        using var conn = Open();
        using var cmd = new MySqlCommand($"SELECT balance FROM {Ledger.CollectionName} WHERE id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public decimal Total() => _ids.Sum(Balance);

    public int CountBookings()
    {
        using var conn = Open();
        using var cmd = new MySqlCommand(
            $"SELECT COUNT(*) FROM {Booking.CollectionName} WHERE SourceId IN ({IdList()}) OR DestinationId IN ({IdList()})", conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Dispose()
    {
        if (_ids.Count == 0)
        {
            return;
        }

        using var conn = Open();
        using var cmd = new MySqlCommand(
            $"DELETE FROM {Booking.CollectionName} WHERE SourceId IN ({IdList()}) OR DestinationId IN ({IdList()}); " +
            $"DELETE FROM {Ledger.CollectionName} WHERE id IN ({IdList()});", conn);
        cmd.ExecuteNonQuery();
    }

    private string IdList() => string.Join(",", _ids);

    private MySqlConnection Open()
    {
        var conn = new MySqlConnection(connectionString);
        conn.Open();
        return conn;
    }
}
