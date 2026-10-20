using BankingDemo.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankingDemo.Api.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();

        EnsureDataDirectoryExists(db);
        await db.Database.EnsureCreatedAsync();

        if (await db.Accounts.AnyAsync())
        {
            return;
        }

        var bank = scope.ServiceProvider.GetRequiredService<BankService>();
        var alice = await bank.OpenAccountAsync("Alice Anderson", 2_500m);
        var bob = await bank.OpenAccountAsync("Bob Brown", 1_000m);
        await bank.OpenAccountAsync("Carol Chen", 7_250m);

        await bank.DepositAsync(alice.Id, 320.50m, "Paycheck");
        await bank.WithdrawAsync(bob.Id, 45.99m, "Groceries");
        await bank.TransferAsync(alice.Id, bob.Id, 150m, "Concert tickets");
    }

    private static void EnsureDataDirectoryExists(BankDbContext db)
    {
        var dataSource = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
