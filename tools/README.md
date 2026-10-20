# Tools and settings for Chainguard Libraries for .NET

Configuration and example commands for consuming
[Chainguard Libraries for .NET](https://edu.chainguard.dev/chainguard/libraries/dotnet/).
Use them to confirm access from a workstation or CI job, then point a real build
at the same feed. The [`dotnet-bank`](../dotnet-bank/README.md) demo in this
repository shows the same setup in a working application.

## In this folder

| Resource | Purpose |
|---|---|
| [`access-examples.md`](access-examples.md) | `chainctl` command examples — entitlements, pull tokens, and policies. Start here to set up credentials. See [Authentication](#authentication). |
| `download-single-file-curl.sh` | Download a single package archive with `curl` to confirm direct access. See [Test access](#test-access). |
| `restore-test-dotnet.sh` | Restore a throwaway project through the feed with the `dotnet` CLI. See [Test access](#test-access). |

## How .NET access works

Chainguard Libraries for .NET is an authenticated NuGet v3 feed. You point NuGet
at it and it serves eligible upstream NuGet package versions under your
organization's policy, malware, and cooldown controls.

Access needs two things:

* The feed URL, `https://libraries.cgr.dev/dotnet/v3/index.json`, configured as a
  NuGet package source.
* A Chainguard Libraries pull token — the identity ID as the feed username and
  the token value as the password, as described in
  [Authentication](#authentication).

NuGet reads both from a `nuget.config` package source. The demo's
[`nuget.config`](../dotnet-bank/nuget.config) is a working example that takes the
credentials from environment variables at restore time:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="chainguard" value="https://libraries.cgr.dev/dotnet/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <chainguard>
      <add key="Username" value="%CHAINGUARD_DOTNET_IDENTITY_ID%" />
      <add key="ClearTextPassword" value="%CHAINGUARD_DOTNET_TOKEN%" />
    </chainguard>
  </packageSourceCredentials>
</configuration>
```

## Authentication

Create a Chainguard Libraries pull token and export it with `-o env` — see
[`access-examples.md`](access-examples.md) for the full `chainctl` commands,
including entitlements and organization-scoped tokens:

```shell
eval "$(chainctl auth pull-token --repository=dotnet -o env)"
```

This sets `CHAINGUARD_DOTNET_IDENTITY_ID` and `CHAINGUARD_DOTNET_TOKEN`. The
matching `%CHAINGUARD_DOTNET_IDENTITY_ID%` and `%CHAINGUARD_DOTNET_TOKEN%`
placeholders in the package source are substituted from these variables at
restore time, so nothing secret is written into the config file. In CI, set
them from the secret store.

As an alternative to a hand-written `nuget.config`, `chainctl` can write the
feed and credentials into your NuGet configuration directly:

```shell
chainctl auth configure nuget --pull-token
```

## Test access

Run these checks once the credentials are exported. They confirm the feed serves
package bytes to your credentials before you point a real build at it. Two
scripts in this folder package the checks, each with a default package and an
optional argument to override it:

```shell
eval "$(chainctl auth pull-token --repository=dotnet -o env)"

# Download a single package archive with curl.
./download-single-file-curl.sh
./download-single-file-curl.sh fluentvalidation 12.1.1

# Restore a throwaway project through the feed with the dotnet CLI.
./restore-test-dotnet.sh
./restore-test-dotnet.sh Serilog 4.3.0
```

The following sections explain the same two checks with raw commands, if you
prefer to run them directly rather than through the scripts.

Fetch the NuGet service index to confirm the feed answers your credentials, then
a single package archive from the flat container:

```shell
curl -fsS -u "$CHAINGUARD_DOTNET_IDENTITY_ID:$CHAINGUARD_DOTNET_TOKEN" \
  "https://libraries.cgr.dev/dotnet/v3/index.json"

curl -fsS -L -u "$CHAINGUARD_DOTNET_IDENTITY_ID:$CHAINGUARD_DOTNET_TOKEN" \
  "https://libraries.cgr.dev/dotnet/v3/flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg" \
  -o newtonsoft.json.13.0.3.nupkg
```

A `401 Unauthorized` means the token is missing or expired; create a fresh one.
A `403 Forbidden` means the token is valid but your organization is not cleared
to pull. Restore the demo through the feed into an isolated package folder, so
the check cannot be served from your global cache:

```shell
cd ../dotnet-bank
dotnet nuget locals http-cache --clear
NUGET_PACKAGES="$(mktemp -d)" \
  dotnet restore BankingDemo.slnx --configfile nuget.config
```

Setting `NUGET_PACKAGES` to a temporary directory keeps your shared global
package folder untouched, so a successful restore proves the packages came from
the configured feed rather than an existing local copy.

## Direct access or a repository manager

The preceding configuration is direct access — every workstation and CI job
authenticates to `libraries.cgr.dev/dotnet` with a pull token. That suits
evaluation, small teams, and isolated CI jobs, and it needs per-workstation
setup.

For organization-wide use, proxy the feed through a repository manager such as
JFrog Artifactory or Sonatype Nexus instead. Configure one NuGet v3 remote that
points at `https://libraries.cgr.dev/dotnet/v3/index.json` with the pull-token
credentials, and point your builds at the manager rather than at Chainguard
directly. Refer to the
[Chainguard Libraries for .NET documentation](https://edu.chainguard.dev/chainguard/libraries/dotnet/)
for the repository manager setup.
