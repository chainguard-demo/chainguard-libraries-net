using BankingDemo.Api.Domain;

namespace BankingDemo.Api.Endpoints;

public record OpenAccountRequest(string OwnerName, decimal InitialDeposit);
public record MoneyRequest(decimal Amount, string? Description);
public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Description);

public record AccountResponse(Guid Id, string AccountNumber, string OwnerName, decimal Balance, DateTime CreatedAtUtc)
{
    public static AccountResponse From(Account a) => new(a.Id, a.AccountNumber, a.OwnerName, a.Balance, a.CreatedAtUtc);
}

public record TransactionResponse(
    Guid Id, Guid AccountId, string Type, decimal Amount, decimal BalanceAfter,
    string? Description, Guid? CounterpartyAccountId, DateTime TimestampUtc)
{
    public static TransactionResponse From(Transaction t) => new(
        t.Id, t.AccountId, t.Type.ToString(), t.Amount, t.BalanceAfter,
        t.Description, t.CounterpartyAccountId, t.TimestampUtc);
}

public record TransferResponse(TransactionResponse Debit, TransactionResponse Credit);
