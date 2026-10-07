using System.Data;
using Bank.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Bank.DbAccess.Repositories;

public class LedgerRepository(IOptions<DatabaseSettings> databaseSettings, ILogger<LedgerRepository> logger) : ILedgerRepository
{
    // Anzahl Versuche bei Deadlock (1213) oder Lock-Wait-Timeout (1205), wie in BookingRepository.Book().
    public const int MaxDeleteAttempts = 3;

    private readonly DatabaseSettings _databaseSettings = databaseSettings.Value;

    public void Book(decimal amount, Ledger from, Ledger to)
    {
        from.Balance -= amount;
        Update(from);
        // Complicate calculations
        Thread.Sleep(250);
        to.Balance += amount;
        Update(to);
    }
    
    public decimal GetTotalMoney()
    {
        const string query = $"SELECT SUM(balance) AS TotalBalance FROM {Ledger.CollectionName}";
        decimal totalBalance = 0;

        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand(query, conn);
        var result = cmd.ExecuteScalar();
        if (result != DBNull.Value)
        {
            totalBalance = Convert.ToDecimal(result);
        }

        return totalBalance;
    }

    public IEnumerable<Ledger> GetAllLedgers()
    {
        var allLedgers = new List<Ledger>();

        const string query = $"SELECT id, name, balance FROM {Ledger.CollectionName} ORDER BY name";
        bool worked;
        do
        {
            worked = true;
            using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                using var cmd = new MySqlCommand(query, conn, transaction);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(reader.GetOrdinal("id"));
                    var name = reader.GetString(reader.GetOrdinal("name"));
                    var balance = reader.GetDecimal(reader.GetOrdinal("balance"));

                    allLedgers.Add(new Ledger { Id = id, Name = name, Balance = balance });
                }
            }
            catch (Exception ex)
            {
                // Attempt to roll back the transaction.
                try
                {
                    transaction.Rollback();
                    if (ex.GetType() != typeof(Exception))
                        worked = false;
                }
                catch (Exception ex2)
                {
                    // Handle any errors that may have occurred on the server that would cause the rollback to fail.
                    if (ex2.GetType() != typeof(Exception))
                        worked = false;
                }
            }
        } while (!worked);

        return allLedgers;
    }
    
    public Ledger? SelectOne(int id)
    {
        Ledger? retLedger = null;
        bool worked;

        do
        {
            worked = true;
            using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                retLedger = SelectOne(id, conn, transaction);
            }
            catch (Exception ex)
            {
                // Attempt to roll back the transaction.
                try
                {
                    transaction.Rollback();
                    if (ex.GetType() != typeof(Exception))
                        worked = false;
                }
                catch (Exception ex2)
                {
                    // Handle any errors that may have occurred on the server that would cause the rollback to fail.
                    if (ex2.GetType() != typeof(Exception))
                        worked = false;
                }
            }
        } while (!worked);

        return retLedger;
    }

    public Ledger SelectOne(int id, MySqlConnection conn, MySqlTransaction? transaction)
    {
        const string query = $"SELECT id, name, balance FROM {Ledger.CollectionName} WHERE id=@Id";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            throw new Exception($"No Ledger with id {id}");

        var ordId = reader.GetInt32(reader.GetOrdinal("id"));
        var ordName = reader.GetString(reader.GetOrdinal("name"));
        var ordBalance = reader.GetDecimal(reader.GetOrdinal("balance"));

        return new Ledger { Id = ordId, Name = ordName, Balance = ordBalance };
    }

    public void Update(Ledger ledger, MySqlConnection conn, MySqlTransaction? transaction)
    {
        const string query = $"UPDATE {Ledger.CollectionName} SET name=@Name, balance=@Balance WHERE id=@Id";
        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Name", ledger.Name);
        cmd.Parameters.AddWithValue("@Balance", ledger.Balance);
        cmd.Parameters.AddWithValue("@Id", ledger.Id);
        
        cmd.ExecuteNonQuery();
    }

    public void Update(Ledger ledger)
    {
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        Update(ledger, conn, null);
    }
    
    public decimal? GetBalance(int ledgerId, MySqlConnection conn, MySqlTransaction transaction)
    {
        const string query = $"SELECT balance FROM {Ledger.CollectionName} WHERE id=@Id";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Id", ledgerId);
        var result = cmd.ExecuteScalar();
        if (result != DBNull.Value)
        {
            return Convert.ToDecimal(result);
        }

        return null;
    }

    public DeleteLedgerResult Delete(int id)
    {
        for (var attempt = 1; attempt <= MaxDeleteAttempts; attempt++)
        {
            try
            {
                return TryDelete(id);
            }
            catch (MySqlException ex) when (ex.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout)
            {
                // Die Transaktion wurde von der DB abgebrochen und automatisch zurückgerollt. Das Löschen wird neu gestartet.
                logger.LogWarning("Löschen von Konto {Id} Versuch {Attempt}/{MaxAttempts} fehlgeschlagen ({ErrorCode}).",
                    id, attempt, MaxDeleteAttempts, ex.ErrorCode);
            }
        }

        logger.LogError("Löschen von Konto {Id} nach {MaxAttempts} Versuchen abgebrochen.", id, MaxDeleteAttempts);
        return DeleteLedgerResult.RetriesExhausted;
    }

    private DeleteLedgerResult TryDelete(int id)
    {
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        // Read Committed reicht wie beim Buchen, weil das Konto mit FOR UPDATE gesperrt wird (siehe README, Zusatz-Feature 1).
        using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        // Book() sperrt das Konto ebenfalls, eine laufende Buchung und das Löschen warten also aufeinander.
        decimal? balance;
        using (var cmd = new MySqlCommand($"SELECT balance FROM {Ledger.CollectionName} WHERE id=@Id FOR UPDATE", conn, transaction))
        {
            cmd.Parameters.AddWithValue("@Id", id);
            var result = cmd.ExecuteScalar();
            balance = result is null or DBNull ? null : Convert.ToDecimal(result);
        }

        if (balance is null)
        {
            transaction.Rollback();
            return DeleteLedgerResult.LedgerNotFound;
        }

        // Sonst würde das Geld auf dem Konto im Total fehlen.
        if (balance != 0)
        {
            transaction.Rollback();
            return DeleteLedgerResult.HasBalance;
        }

        // Die Historie soll erhalten bleiben, der Fremdschlüssel Bookings -> Ledgers würde das Löschen sonst mit einem DB-Fehler abbrechen.
        using (var cmd = new MySqlCommand($"SELECT COUNT(*) FROM {Booking.CollectionName} WHERE SourceId=@Id OR DestinationId=@Id", conn, transaction))
        {
            cmd.Parameters.AddWithValue("@Id", id);
            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
            {
                transaction.Rollback();
                return DeleteLedgerResult.HasBookings;
            }
        }

        using (var cmd = new MySqlCommand($"DELETE FROM {Ledger.CollectionName} WHERE id=@Id", conn, transaction))
        {
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return DeleteLedgerResult.Success;
    }
}