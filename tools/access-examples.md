# Example commands for Chainguard Libraries for .NET access

`chainctl` commands for setting up and managing access to
[Chainguard Libraries for .NET](https://edu.chainguard.dev/chainguard/libraries/dotnet/).
The pull-token commands produce the credentials the `dotnet-bank` demo and the
configuration in [`README.md`](README.md) depend on. The entitlement and policy
commands are for administrators setting up an organization.

## Prerequisites

Authenticate to the Chainguard platform first:

```shell
chainctl auth login
```

Creating entitlements and policies requires appropriate permissions on the
organization. Generating a pull token requires an active libraries entitlement.

Most users belong to a single organization, so `chainctl` selects it
automatically or prompts you to choose interactively. Add `--parent=<org>` to
any command that follows only if you belong to more than one organization and
need it to run non-interactively, for example in CI.

## Entitlements

List existing entitlements:

```shell
chainctl libraries entitlements list
```

Create an entitlement for .NET:

```shell
chainctl libraries entitlements create --ecosystems=DOTNET
```

`--ecosystems` is required and takes a comma-separated list, for example
`DOTNET,JAVA`. The .NET entitlement serves eligible upstream NuGet package
versions under Chainguard security controls alongside any Chainguard-built
versions.

## Pull tokens

A pull token provides the credentials NuGet authenticates with. The token has
two parts: an identity ID used as the NuGet feed username, and a token value
used as the password.

Export the token with `-o env`, which emits shell commands that set
`CHAINGUARD_DOTNET_IDENTITY_ID` and `CHAINGUARD_DOTNET_TOKEN`, the two variables
the `nuget.config` files read:

```shell
eval "$(chainctl auth pull-token --repository=dotnet -o env)"
```

The `dotnet-bank` demo's [`env.sh`](../dotnet-bank/env.sh) wraps this with the
`--parent` and `--ttl` flags. If you need the raw fields instead, `-o json`
prints `identity_id` and `token`.

`--repository` must be one of `oci`, `apk`, `java`, `python`, `javascript`,
`go`, or `dotnet`. Control validity with `--ttl`, for example `--ttl=24h`; the
maximum is `8760h`, one year.

As an alternative to editing `nuget.config` yourself, `chainctl` can write the
feed and credentials into your NuGet configuration directly:

```shell
chainctl auth configure nuget --pull-token
```

If you are a member of multiple organizations the preceding commands must add
the `--parent` parameter with the name of your organization:

```shell
chainctl auth pull-token --repository=dotnet --parent=chainguard.edu -o json
```

## Policies

Policies control the cooldown window and package gating for an ecosystem. A
newly created policy is inactive until it is activated for an ecosystem.

Create a policy with no cooldown:

```shell
chainctl libraries policy create --name=no-cooldown --cooldown-days=0
```

Show the policy details:

```shell
chainctl libraries policy describe no-cooldown
```

Activate it for .NET in enforcing mode:

```shell
chainctl libraries policy enable --policy=no-cooldown --ecosystem=DOTNET --mode=ENFORCE
```

List policies and their bindings to ecosystems:

```shell
chainctl libraries policy list
chainctl libraries policy bindings list
```
