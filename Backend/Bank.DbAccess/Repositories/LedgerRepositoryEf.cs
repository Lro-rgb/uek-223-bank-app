using System.Data;
using Bank.Core.Models;
using Bank.DbAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Bank.DbAccess.Repositories;

// ToDo Sobald du das LedgerRepository verstehst, kannst du es löschen und alles mit dem LedgerRepositoryEf machen.
// So nutzt die ganze Applikation nur noch Entity Framework und das Testing wird auch einfacher werden.
public class LedgerRepositoryEf(AppDbContext context) : ILedgerRepositoryEf
{
    public string Book(decimal amount, Ledger from, Ledger to)
    {
        from.Balance -= amount;
        context.Ledgers.Update(from);

        // Simulate complicated calculations
        Thread.Sleep(250);

        to.Balance += amount;
        context.Ledgers.Update(to);

        context.SaveChanges();
        
        return ".";
    }
    
    public string BookForCli(decimal amount, Ledger from, Ledger to)
    {
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        
        try
        {
            var fromLedger = context.Ledgers.FirstOrDefault(l => l.Id == from.Id);
            if (fromLedger == null) throw new ArgumentNullException(nameof(from), "From ledger not found.");

            fromLedger.Balance -= amount;
            context.Ledgers.Update(fromLedger);

            // Simulate complicated calculations
            Thread.Sleep(250);

            var toLedger = context.Ledgers.FirstOrDefault(l => l.Id == to.Id);
            if (toLedger == null) throw new ArgumentNullException(nameof(to), "To ledger not found.");

            toLedger.Balance += amount;
            context.Ledgers.Update(toLedger);

            context.SaveChanges();
            transaction.Commit();
            
            return ".";
        }
        catch (Exception)
        {
            try
            {
                transaction.Rollback();
                return "R";
            }
            catch (Exception)
            {
                return "E";
            }
        }
    }
    
    public IEnumerable<Ledger> GetAllLedgers()
    {
        return context.Ledgers.ToList();
    }

    public decimal GetTotalMoney()
    {
        return context.Ledgers.Sum(l => l.Balance);
    }
    
    public Ledger? SelectOne(int id)
    {
        return context.Ledgers.AsNoTracking().FirstOrDefault(l => l.Id == id);
    }
    
    public void Update(Ledger ledger)
    {
        if (!context.Ledgers.Any(l => l.Id == ledger.Id))
            throw new ArgumentNullException(nameof(ledger), "Ledger not found.");
    
        context.Ledgers.Update(ledger);
        context.SaveChanges();
    }
    
    public decimal? GetBalance(int ledgerId)
    {
        return context.Ledgers
            .AsNoTracking()
            .FirstOrDefault(l => l.Id == ledgerId)
            ?.Balance;
    }
}