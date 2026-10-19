# Banking Demo (.NET 10)

A small, demo-only banking app: an ASP.NET Core minimal API plus a single-page web UI. You can open accounts, deposit, withdraw, transfer between accounts, and view transaction history. Data lives in a SQLite file that is seeded with a few demo accounts on first start.

> ⚠️ Demo purposes only. There is no authentication, and it should not be used with real money.

**Stack:** ASP.NET Core 10 · EF Core + SQLite · FluentValidation · Serilog · Swashbuckle (Swagger UI) · xUnit

```
├── src/BankingDemo.Api/        # API, domain, EF Core persistence, static UI (wwwroot)
├── tests/BankingDemo.Tests/    # Integration tests (WebApplicationFactory)
├── Dockerfile                  # Multi-stage, switchable Chainguard / Microsoft base images
├── nuget.config                # Default: nuget.org
└── nuget.chainguard.config     # Alternate: restore from $NUGET_SOURCE_URL (Chainguard Libraries / Artifactory)
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

## Run with Docker

The base images are selected with build args. **Chainguard is the default.**

```bash
# Chainguard images (cgr.dev/chainguard/dotnet-sdk + aspnet-runtime)
docker build -t banking-demo:chainguard .

# Microsoft images (mcr.microsoft.com/dotnet/sdk + aspnet)
docker build -t banking-demo:microsoft \
  --build-arg SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0 \
  --build-arg RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0 \
  --build-arg APP_UID=1654 \
  .

docker run --rm -p 8080:8080 banking-demo:chainguard
```

Both images run as a non-root user and listen on port 8080. To keep data across container restarts, mount a volume:

```bash
docker run --rm -p 8080:8080 -v bank-data:/app/data banking-demo:chainguard
```

| Build arg | Default | Purpose |
|---|---|---|
| `SDK_IMAGE` | `cgr.dev/chainguard/dotnet-sdk:latest` | Build stage image |
| `RUNTIME_IMAGE` | `cgr.dev/chainguard/aspnet-runtime:latest` | Runtime image |
| `APP_UID` | `65532` | Runtime UID (`65532` = Chainguard `nonroot`, `1654` = Microsoft `app`) |
| `NUGET_SOURCE_URL` | _(empty → nuget.org)_ | Alternate NuGet v3 feed (see below) |

## Restoring NuGet packages from Chainguard Libraries (or Artifactory)

By default, packages restore from nuget.org (`nuget.config`). To restore **only** from another NuGet v3 feed, such as Chainguard Libraries or an Artifactory/Nexus remote that proxies it, use `nuget.chainguard.config`. It reads the feed URL and credentials from environment variables at restore time, so no secrets are written to disk.

### Locally

```bash
export NUGET_SOURCE_URL="https://<your-feed>/index.json"
export NUGET_USERNAME="<username>"
export NUGET_PASSWORD="<token>"

dotnet nuget locals all --clear        # optional: make sure nothing comes from the local cache
dotnet restore --configfile nuget.chainguard.config
dotnet run --project src/BankingDemo.Api --no-restore
```

### In Docker

Pass the URL as a build arg and the credentials as [BuildKit secrets](https://docs.docker.com/build/building/secrets/). The secrets are mounted only for the restore step and are never stored in an image layer.

```bash
export NUGET_USERNAME="<username>" NUGET_PASSWORD="<token>"

docker build -t banking-demo:chainguard-libs \
  --build-arg NUGET_SOURCE_URL="https://<your-feed>/index.json" \
  --secret id=nuget_username,env=NUGET_USERNAME \
  --secret id=nuget_password,env=NUGET_PASSWORD \
  .
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
