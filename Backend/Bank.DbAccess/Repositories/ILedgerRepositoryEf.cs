using Bank.Core.Models;
using MySqlConnector;

namespace Bank.DbAccess.Repositories;

public interface ILedgerRepositoryEf
{
    IEnumerable<Ledger> GetAllLedgers();
    public string BookForCli(decimal amount, Ledger from, Ledger to);
    public string Book(decimal amount, Ledger from, Ledger to);
    decimal GetTotalMoney();
    Ledger? SelectOne(int id);
    void Update(Ledger ledger);
    decimal? GetBalance(int ledgerId);
}