namespace BankingDemo.Api.Domain;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string AccountNumber { get; set; }
    public required string OwnerName { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Transaction> Transactions { get; set; } = [];
}
