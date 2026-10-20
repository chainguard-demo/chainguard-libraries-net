using System.Net;
using System.Net.Http.Json;
using BankingDemo.Api.Endpoints;

namespace BankingDemo.Tests;

public class BankApiTests(BankApiFactory factory) : IClassFixture<BankApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<AccountResponse> OpenAccountAsync(string owner, decimal deposit)
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new OpenAccountRequest(owner, deposit));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }

    private async Task<decimal> GetBalanceAsync(Guid id) =>
        (await _client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{id}"))!.Balance;

    [Fact]
    public async Task Seeds_demo_accounts()
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountResponse>>("/api/accounts");
        Assert.Contains(accounts!, a => a.OwnerName == "Alice Anderson");
    }

    [Fact]
    public async Task Open_account_records_initial_deposit()
    {
        var account = await OpenAccountAsync("Dana Diaz", 100m);

        var transactions = await _client.GetFromJsonAsync<List<TransactionResponse>>($"/api/accounts/{account.Id}/transactions");

        Assert.Equal(100m, account.Balance);
        var tx = Assert.Single(transactions!);
        Assert.Equal("Deposit", tx.Type);
    }

    [Fact]
    public async Task Deposit_and_withdraw_update_balance()
    {
        var account = await OpenAccountAsync("Eve Evans", 50m);

        await _client.PostAsJsonAsync($"/api/accounts/{account.Id}/deposit", new MoneyRequest(25.25m, "Gift"));
        await _client.PostAsJsonAsync($"/api/accounts/{account.Id}/withdraw", new MoneyRequest(10m, null));

        Assert.Equal(65.25m, await GetBalanceAsync(account.Id));
    }

    [Fact]
    public async Task Withdraw_more_than_balance_is_rejected()
    {
        var account = await OpenAccountAsync("Frank Fox", 20m);

        var response = await _client.PostAsJsonAsync($"/api/accounts/{account.Id}/withdraw", new MoneyRequest(20.01m, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(20m, await GetBalanceAsync(account.Id));
    }

    [Fact]
    public async Task Transfer_moves_money_between_accounts()
    {
        var from = await OpenAccountAsync("Grace Green", 300m);
        var to = await OpenAccountAsync("Hank Hill", 0m);

        var response = await _client.PostAsJsonAsync("/api/transfers", new TransferRequest(from.Id, to.Id, 120m, "Rent"));

        response.EnsureSuccessStatusCode();
        Assert.Equal(180m, await GetBalanceAsync(from.Id));
        Assert.Equal(120m, await GetBalanceAsync(to.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1.001)]
    public async Task Invalid_amounts_fail_validation(decimal amount)
    {
        var account = await OpenAccountAsync("Ivy Irwin", 10m);

        var response = await _client.PostAsJsonAsync($"/api/accounts/{account.Id}/deposit", new MoneyRequest(amount, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_account_returns_404()
    {
        var response = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_is_healthy()
    {
        Assert.Equal("Healthy", await _client.GetStringAsync("/health"));
    }
}
