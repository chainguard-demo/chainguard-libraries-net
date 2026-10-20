using BankingDemo.Api.Persistence;
using BankingDemo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BankingDemo.Api.Services;

public class BankService(BankDbContext db, ILogger<BankService> logger)
{
    public Task<List<Account>> GetAccountsAsync() =>
        db.Accounts.AsNoTracking().OrderBy(a => a.AccountNumber).ToListAsync();

    public async Task<Account> GetAccountAsync(Guid id) =>
        await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id)
        ?? throw new AccountNotFoundException(id);

    public async Task<List<Transaction>> GetTransactionsAsync(Guid accountId)
    {
        await GetAccountAsync(accountId);
        return await db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TimestampUtc)
            .ToListAsync();
    }

    public async Task<Account> OpenAccountAsync(string ownerName, decimal initialDeposit)
    {
        var account = new Account
        {
            AccountNumber = await GenerateAccountNumberAsync(),
            OwnerName = ownerName.Trim()
        };
        db.Accounts.Add(account);

        if (initialDeposit > 0)
        {
            Apply(account, TransactionType.Deposit, initialDeposit, "Initial deposit");
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Opened account {AccountNumber} for {Owner} with {Deposit:0.00}",
            account.AccountNumber, account.OwnerName, initialDeposit);
        return account;
    }

    public async Task<Transaction> DepositAsync(Guid accountId, decimal amount, string? description)
    {
        var account = await FindTrackedAsync(accountId);
        var transaction = Apply(account, TransactionType.Deposit, amount, description);
        await db.SaveChangesAsync();
        logger.LogInformation("Deposited {Amount:0.00} into {AccountNumber}", amount, account.AccountNumber);
        return transaction;
    }

    public async Task<Transaction> WithdrawAsync(Guid accountId, decimal amount, string? description)
    {
        var account = await FindTrackedAsync(accountId);
        EnsureSufficientFunds(account, amount);
        var transaction = Apply(account, TransactionType.Withdrawal, amount, description);
        await db.SaveChangesAsync();
        logger.LogInformation("Withdrew {Amount:0.00} from {AccountNumber}", amount, account.AccountNumber);
        return transaction;
    }

    public async Task<(Transaction Debit, Transaction Credit)> TransferAsync(
        Guid fromAccountId, Guid toAccountId, decimal amount, string? description)
    {
        if (fromAccountId == toAccountId)
        {
            throw new InvalidTransferException("Cannot transfer to the same account.");
        }

        await using var dbTransaction = await db.Database.BeginTransactionAsync();

        var from = await FindTrackedAsync(fromAccountId);
        var to = await FindTrackedAsync(toAccountId);
        EnsureSufficientFunds(from, amount);

        var debit = Apply(from, TransactionType.TransferOut, amount, description ?? $"Transfer to {to.AccountNumber}", to.Id);
        var credit = Apply(to, TransactionType.TransferIn, amount, description ?? $"Transfer from {from.AccountNumber}", from.Id);

        await db.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        logger.LogInformation("Transferred {Amount:0.00} from {From} to {To}", amount, from.AccountNumber, to.AccountNumber);
        return (debit, credit);
    }

    private Transaction Apply(Account account, TransactionType type, decimal amount, string? description,
        Guid? counterparty = null)
    {
        account.Balance += type is TransactionType.Deposit or TransactionType.TransferIn ? amount : -amount;

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Type = type,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = description,
            CounterpartyAccountId = counterparty
        };
        db.Transactions.Add(transaction);
        return transaction;
    }

    private static void EnsureSufficientFunds(Account account, decimal amount)
    {
        if (account.Balance < amount)
        {
            throw new InsufficientFundsException(account.AccountNumber, account.Balance, amount);
        }
    }

    private async Task<Account> FindTrackedAsync(Guid id) =>
        await db.Accounts.FirstOrDefaultAsync(a => a.Id == id) ?? throw new AccountNotFoundException(id);

    private async Task<string> GenerateAccountNumberAsync()
    {
        while (true)
        {
            var number = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
            if (!await db.Accounts.AnyAsync(a => a.AccountNumber == number))
            {
                return number;
            }
        }
    }
}
