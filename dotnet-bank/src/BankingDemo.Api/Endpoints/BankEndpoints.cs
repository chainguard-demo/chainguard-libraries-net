using BankingDemo.Api.Services;

namespace BankingDemo.Api.Endpoints;

public static class BankEndpoints
{
    public static IEndpointRouteBuilder MapBankEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/accounts").WithTags("Accounts");

        accounts.MapGet("/", async (BankService bank) =>
            (await bank.GetAccountsAsync()).Select(AccountResponse.From));

        accounts.MapGet("/{id:guid}", async (Guid id, BankService bank) =>
            AccountResponse.From(await bank.GetAccountAsync(id)))
            .WithName("GetAccount");

        accounts.MapPost("/", async (OpenAccountRequest request, BankService bank) =>
        {
            var account = await bank.OpenAccountAsync(request.OwnerName, request.InitialDeposit);
            return Results.CreatedAtRoute("GetAccount", new { id = account.Id }, AccountResponse.From(account));
        }).AddEndpointFilter<ValidationFilter<OpenAccountRequest>>();

        accounts.MapGet("/{id:guid}/transactions", async (Guid id, BankService bank) =>
            (await bank.GetTransactionsAsync(id)).Select(TransactionResponse.From));

        accounts.MapPost("/{id:guid}/deposit", async (Guid id, MoneyRequest request, BankService bank) =>
            TransactionResponse.From(await bank.DepositAsync(id, request.Amount, request.Description)))
            .AddEndpointFilter<ValidationFilter<MoneyRequest>>();

        accounts.MapPost("/{id:guid}/withdraw", async (Guid id, MoneyRequest request, BankService bank) =>
            TransactionResponse.From(await bank.WithdrawAsync(id, request.Amount, request.Description)))
            .AddEndpointFilter<ValidationFilter<MoneyRequest>>();

        app.MapPost("/api/transfers", async (TransferRequest request, BankService bank) =>
        {
            var (debit, credit) = await bank.TransferAsync(
                request.FromAccountId, request.ToAccountId, request.Amount, request.Description);
            return new TransferResponse(TransactionResponse.From(debit), TransactionResponse.From(credit));
        })
        .WithTags("Transfers")
        .AddEndpointFilter<ValidationFilter<TransferRequest>>();

        return app;
    }
}
