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

    // Anzahl Versuche beim Lesen der Konten, danach geht der Fehler an den Aufrufer.
    public const int MaxReadAttempts = 3;

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
        const string query = $"SELECT id, name, balance FROM {Ledger.CollectionName} ORDER BY name";

        return ReadWithRetry((conn, transaction) =>
        {
            // Pro Versuch eine neue Liste, sonst stünden Konten nach einem Retry doppelt drin.
            var allLedgers = new List<Ledger>();
            using var cmd = new MySqlCommand(query, conn, transaction);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                allLedgers.Add(ReadLedger(reader));
            }

            return allLedgers;
        });
    }

    public Ledger? SelectOne(int id)
    {
        return ReadWithRetry((conn, transaction) => SelectOne(id, conn, transaction));
    }

    public Ledger? SelectOne(int id, MySqlConnection conn, MySqlTransaction? transaction)
    {
        const string query = $"SELECT id, name, balance FROM {Ledger.CollectionName} WHERE id=@Id";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadLedger(reader) : null;
    }

    // Lesen mit Retry wie beim Buchen: Nur Deadlock (1213) und Lock-Wait-Timeout (1205) werden wiederholt, alle anderen
    // Fehler gehen an den Aufrufer. Vorher startete die Schleife bei jedem Fehler neu, ein Konto mit name = NULL hat
    // GET /Ledgers deshalb endlos hängen lassen (externer Test, F1).
    private T ReadWithRetry<T>(Func<MySqlConnection, MySqlTransaction, T> read)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
                conn.Open();
                // Read Committed reicht: Ein einzelnes SELECT liest einen konsistenten, committeten Stand. Serializable würde
                // beim Lesen sperren und Buchungen unnötig warten lassen (README, Isolationslevel).
                using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                var result = read(conn, transaction);
                transaction.Commit();
                return result;
            }
            catch (MySqlException ex) when (IsTransient(ex) && attempt < MaxReadAttempts)
            {
                logger.LogWarning("Lesen der Konten Versuch {Attempt}/{MaxAttempts} fehlgeschlagen ({ErrorCode}).",
                    attempt, MaxReadAttempts, ex.ErrorCode);
            }
        }
    }

    // Ledger.Name ist nullable, GetString würde bei NULL eine Exception werfen (wie in BookingRepository.GetHistory()).
    private static Ledger ReadLedger(MySqlDataReader reader)
    {
        var nameOrdinal = reader.GetOrdinal("name");
        return new Ledger
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
            Balance = reader.GetDecimal(reader.GetOrdinal("balance")),
        };
    }

    private static bool IsTransient(MySqlException ex) =>
        ex.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout;

    public void Update(Ledger ledger, MySqlConnection conn, MySqlTransaction? transaction)
    {
        ExecuteUpdate(ledger, conn, transaction);
    }

    // Schreibt Name und Saldo absolut, ohne Sperre. Nur noch für die Konsolen-Übung (Book() in Bank.Cli), die API ändert
    // den Saldo nie direkt (siehe UpdateName()).
    public bool Update(Ledger ledger)
    {
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        return ExecuteUpdate(ledger, conn, null) > 0;
    }

    // Für PUT /api/v1/Ledgers/{id}: ändert nur den Namen. Der Saldo bleibt unberührt, so kann eine gleichzeitige Buchung
    // nicht überschrieben werden (kein Lost Update). false, wenn es kein Konto mit dieser ID gibt (PUT liefert dann 404).
    public bool UpdateName(int id, string name)
    {
        const string query = $"UPDATE {Ledger.CollectionName} SET name=@Name WHERE id=@Id";
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    // Gibt die Anzahl gefundener Zeilen zurück. MySqlConnector zählt standardmässig gefundene statt geänderte Zeilen
    // (UseAffectedRows=false), ein Update ohne Änderung ergibt also auch 1.
    private static int ExecuteUpdate(Ledger ledger, MySqlConnection conn, MySqlTransaction? transaction)
    {
        const string query = $"UPDATE {Ledger.CollectionName} SET name=@Name, balance=@Balance WHERE id=@Id";
        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Name", ledger.Name);
        cmd.Parameters.AddWithValue("@Balance", ledger.Balance);
        cmd.Parameters.AddWithValue("@Id", ledger.Id);

        return cmd.ExecuteNonQuery();
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