namespace BankingDemo.Api.Domain;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferIn,
    TransferOut
}

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public Guid? CounterpartyAccountId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
