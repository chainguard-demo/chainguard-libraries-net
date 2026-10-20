# Banking Demo (.NET 10)

A small, demo-only banking app: an ASP.NET Core minimal API plus a single-page web UI. You can open accounts, deposit, withdraw, transfer between accounts, and view transaction history. Data lives in a SQLite file that is seeded with a few demo accounts on first start.

> ⚠️ Demo purposes only. There is no authentication, and it should not be used with real money.

**Stack:** ASP.NET Core 10 · EF Core + SQLite · FluentValidation · Serilog · Swashbuckle (Swagger UI) · xUnit

```
├── src/BankingDemo.Api/        # API, domain, EF Core persistence, static UI (wwwroot)
├── tests/BankingDemo.Tests/    # Integration tests (WebApplicationFactory)
├── env.sh                      # Helper script to generate Chainguard Pull Token
├── nuget.config                # Default: Chainguard Libraries for .NET
└── nuget.standard.config       # Alternate: restore from nuget.org
```

## Run locally with the `dotnet` CLI

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/BankingDemo.Api
```

Then open:

| URL | What |
|---|---|
| http://localhost:8080 | Web UI |
| http://localhost:8080/swagger | Swagger UI |
| http://localhost:8080/health | Health check |

The database is created at `src/BankingDemo.Api/data/bank.db`. Delete that folder to reset to the seed data.

Run the tests:

```bash
dotnet test
```

## Restoring NuGet packages from Chainguard Libraries

By default, packages restore from Chainguard Libraries for .NET (`nuget.config`), which reads the feed URL and credentials from environment variables at restore time. To restore from nuget.org instead, for comparison, use `nuget.standard.config`.

### Locally

```bash
source env.sh your_chainguard_org

# or export the credentials directly:

eval "$(chainctl auth pull-token --repository=dotnet -o env)"

dotnet nuget locals all --clear        # optional: make sure nothing comes from the local cache
dotnet restore --configfile nuget.config
dotnet run --project src/BankingDemo.Api --no-restore
```

## API

| Method | Route | Body |
|---|---|---|
| `GET` | `/api/accounts` | |
| `GET` | `/api/accounts/{id}` | |
| `POST` | `/api/accounts` | `{ "ownerName": "Dana", "initialDeposit": 100 }` |
| `GET` | `/api/accounts/{id}/transactions` | |
| `POST` | `/api/accounts/{id}/deposit` | `{ "amount": 25.00, "description": "Gift" }` |
| `POST` | `/api/accounts/{id}/withdraw` | `{ "amount": 10.00 }` |
| `POST` | `/api/transfers` | `{ "fromAccountId": "…", "toAccountId": "…", "amount": 50 }` |
| `GET` | `/health` | |

Errors are returned as RFC 7807 problem details: `400` for validation errors, `404` for an unknown account, and `422` for insufficient funds.
