using System.Data;
using Bank.Core.Helper;
using Bank.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Bank.DbAccess.Repositories;

public class BookingRepository(IOptions<DatabaseSettings> databaseSettings, ILogger<BookingRepository> logger) : IBookingRepository
{
    // Anzahl Versuche bei Deadlock (1213) oder Lock-Wait-Timeout (1205), danach wird abgebrochen (siehe README, Massnahme 3).
    public const int MaxAttempts = 3;

    private readonly DatabaseSettings _databaseSettings = databaseSettings.Value;

    // Nur für Tests: wird nach der Saldo-Prüfung und vor den Updates aufgerufen. Damit kann ein Test zwei Buchungen
    // gezielt ineinander verschachteln, statt auf Zufall zu hoffen (BookingConcurrencyTests).
    internal Action? AfterBalanceCheck { get; set; }

    public BookingResult Book(int sourceLedgerId, int destinationLedgerId, decimal amount)
    {
        var validation = BookingValidator.Validate(sourceLedgerId, destinationLedgerId, amount);
        if (validation != BookingResult.Success)
        {
            return validation;
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return TryBook(sourceLedgerId, destinationLedgerId, amount);
            }
            catch (MySqlException ex) when (IsTransient(ex))
            {
                // Die Transaktion wurde von der DB abgebrochen und automatisch zurückgerollt. Die ganze Buchung wird neu gestartet.
                logger.LogWarning("Buchung {Source} -> {Destination} Versuch {Attempt}/{MaxAttempts} fehlgeschlagen ({ErrorCode}).",
                    sourceLedgerId, destinationLedgerId, attempt, MaxAttempts, ex.ErrorCode);
            }
        }

        logger.LogError("Buchung {Source} -> {Destination} nach {MaxAttempts} Versuchen abgebrochen.",
            sourceLedgerId, destinationLedgerId, MaxAttempts);
        return BookingResult.RetriesExhausted;
    }

    public IReadOnlyList<BookingHistoryEntry>? GetHistory(int ledgerId)
    {
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();

        using (var cmd = new MySqlCommand($"SELECT COUNT(*) FROM {Ledger.CollectionName} WHERE id=@Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", ledgerId);
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                return null;
            }
        }

        // Ein einzelnes SELECT liest einen konsistenten Stand: Eine laufende Buchung ist erst nach ihrem Commit sichtbar,
        // dann aber Saldo und Historien-Eintrag zusammen (beides in derselben Transaktion, siehe Book()).
        const string query =
            $"SELECT b.Id, b.SourceId, s.name AS SourceName, b.DestinationId, d.name AS DestinationName, b.Amount, b.CreatedAt " +
            $"FROM {Booking.CollectionName} b " +
            $"JOIN {Ledger.CollectionName} s ON s.id = b.SourceId " +
            $"JOIN {Ledger.CollectionName} d ON d.id = b.DestinationId " +
            $"WHERE b.SourceId=@Id OR b.DestinationId=@Id " +
            $"ORDER BY b.CreatedAt DESC, b.Id DESC";

        using var historyCmd = new MySqlCommand(query, conn);
        historyCmd.Parameters.AddWithValue("@Id", ledgerId);
        using var reader = historyCmd.ExecuteReader();

        var entries = new List<BookingHistoryEntry>();
        while (reader.Read())
        {
            entries.Add(new BookingHistoryEntry
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                SourceId = reader.GetInt32(reader.GetOrdinal("SourceId")),
                SourceName = ReadNullableString(reader, "SourceName"),
                DestinationId = reader.GetInt32(reader.GetOrdinal("DestinationId")),
                DestinationName = ReadNullableString(reader, "DestinationName"),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("CreatedAt")), DateTimeKind.Utc),
            });
        }

        return entries;
    }

    private BookingResult TryBook(int sourceLedgerId, int destinationLedgerId, decimal amount)
    {
        using var conn = new MySqlConnection(_databaseSettings.ConnectionString);
        conn.Open();
        // Read Committed reicht, weil beide Konten mit FOR UPDATE gesperrt werden (siehe README, Isolationslevel für Buchen).
        using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        // Immer zuerst das Konto mit der kleineren ID sperren, damit sich zwei Buchungen nicht gegenseitig blockieren (Deadlock).
        var sourceBalance = (decimal?)null;
        var destinationBalance = (decimal?)null;
        foreach (var ledgerId in new[] { Math.Min(sourceLedgerId, destinationLedgerId), Math.Max(sourceLedgerId, destinationLedgerId) })
        {
            var balance = SelectBalanceForUpdate(ledgerId, conn, transaction);
            if (ledgerId == sourceLedgerId) sourceBalance = balance;
            else destinationBalance = balance;
        }

        if (sourceBalance is null || destinationBalance is null)
        {
            transaction.Rollback();
            return BookingResult.LedgerNotFound;
        }

        // Die Prüfung läuft auf der gesperrten Zeile, niemand kann den Saldo bis zum Commit ändern.
        if (sourceBalance < amount)
        {
            transaction.Rollback();
            return BookingResult.InsufficientFunds;
        }

        AfterBalanceCheck?.Invoke();

        UpdateBalance(sourceLedgerId, -amount, conn, transaction);
        UpdateBalance(destinationLedgerId, amount, conn, transaction);
        InsertBooking(sourceLedgerId, destinationLedgerId, amount, conn, transaction);

        transaction.Commit();
        return BookingResult.Success;
    }

    private static decimal? SelectBalanceForUpdate(int ledgerId, MySqlConnection conn, MySqlTransaction transaction)
    {
        const string query = $"SELECT balance FROM {Ledger.CollectionName} WHERE id=@Id FOR UPDATE";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Id", ledgerId);
        var result = cmd.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToDecimal(result);
    }

    private static void UpdateBalance(int ledgerId, decimal delta, MySqlConnection conn, MySqlTransaction transaction)
    {
        const string query = $"UPDATE {Ledger.CollectionName} SET balance = balance + @Delta WHERE id=@Id";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@Delta", delta);
        cmd.Parameters.AddWithValue("@Id", ledgerId);
        cmd.ExecuteNonQuery();
    }

    private static void InsertBooking(int sourceLedgerId, int destinationLedgerId, decimal amount, MySqlConnection conn, MySqlTransaction transaction)
    {
        // Historien-Eintrag in derselben Transaktion wie die Saldo-Updates (siehe README, Massnahme 4).
        const string query = $"INSERT INTO {Booking.CollectionName} (SourceId, DestinationId, Amount, CreatedAt) VALUES (@SourceId, @DestinationId, @Amount, @CreatedAt)";

        using var cmd = new MySqlCommand(query, conn, transaction);
        cmd.Parameters.AddWithValue("@SourceId", sourceLedgerId);
        cmd.Parameters.AddWithValue("@DestinationId", destinationLedgerId);
        cmd.Parameters.AddWithValue("@Amount", amount);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        cmd.ExecuteNonQuery();
    }

    // Ledgers.name ist nullable (Ledger.Name ist string?), GetString würde bei NULL eine Exception werfen (500).
    private static string? ReadNullableString(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static bool IsTransient(MySqlException ex) =>
        ex.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout;
}
