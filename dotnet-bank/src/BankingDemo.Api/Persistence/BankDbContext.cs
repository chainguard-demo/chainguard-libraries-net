using BankingDemo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BankingDemo.Api.Persistence;

public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(account =>
        {
            account.HasIndex(a => a.AccountNumber).IsUnique();
            account.Property(a => a.AccountNumber).HasMaxLength(12);
            account.Property(a => a.OwnerName).HasMaxLength(100);
            account.HasMany(a => a.Transactions)
                .WithOne()
                .HasForeignKey(t => t.AccountId);
        });

        modelBuilder.Entity<Transaction>(transaction =>
        {
            transaction.Property(t => t.Type).HasConversion<string>();
            transaction.Property(t => t.Description).HasMaxLength(200);
            transaction.HasIndex(t => new { t.AccountId, t.TimestampUtc });
        });
    }
}
