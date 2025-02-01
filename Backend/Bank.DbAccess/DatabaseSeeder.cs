using Bank.Core.Helper;
using Bank.Core.Models;
using Bank.DbAccess.Data;

namespace Bank.DbAccess;

public class DatabaseSeeder(AppDbContext context) : IDatabaseSeeder
{
    private void SeedLedgers()
    {
        if (context.Ledgers.Any())
        {
            return;
        }

        var moneyProvider = new Random();

        var seedLedgers = new List<Ledger>
        {
            new() { Name = "Manitu AG", Balance = moneyProvider.Next(100, 10001) },
            new() { Name = "Chrysalkis GmbH", Balance = moneyProvider.Next(100, 10001) },
            new() { Name = "Smith & Co KG", Balance = moneyProvider.Next(100, 10001) },
        };

        context.Ledgers.AddRange(seedLedgers);
        context.SaveChanges();
    }

    private void SeedUsers()
    {
        if (context.Users.Any())
        {
            return;
        }

        var seedUsers = new List<User>
        {
            new() { Username = "admin", PasswordHash = PasswordHelper.HashAndSaltPassword("adminpass"), Role = Roles.Administrators },
            new() { Username = "testuser", PasswordHash = PasswordHelper.HashAndSaltPassword("testuserpass"), Role = Roles.Users },
        };

        context.Users.AddRange(seedUsers);
        context.SaveChanges();
    }

    public void Initialize()
    {
        context.Database.EnsureCreated();
    }

    public void Seed()
    {
        SeedLedgers();
        SeedUsers();
    }
}