namespace BankingDemo.Api.Domain;

public class AccountNotFoundException(Guid accountId)
    : Exception($"Account '{accountId}' was not found.");

public class InsufficientFundsException(string accountNumber, decimal balance, decimal requested)
    : Exception($"Account {accountNumber} has insufficient funds: balance {balance:0.00}, requested {requested:0.00}.");

public class InvalidTransferException(string message) : Exception(message);
