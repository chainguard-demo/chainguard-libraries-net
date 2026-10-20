#!/usr/bin/env bash
#
# Restores a single package and its dependencies through Chainguard Libraries
# for .NET to confirm access. It builds a throwaway project in a temporary
# directory, restores into a temporary package folder, and reports success. It
# changes no files in this repository and leaves your shared NuGet caches and
# your global package folder untouched.
#
# Credentials come from a Chainguard Libraries pull token. Export them first
# (see README.md and access-examples.md):
#
#   eval "$(chainctl auth pull-token --repository=dotnet -o env)"
#
# The script writes a nuget.config that references these variables, so the
# token never lands in a file on disk.
#
# Usage: restore-test-dotnet.sh [PACKAGE_ID] [VERSION]
#   PACKAGE_ID  NuGet package id. Defaults to Newtonsoft.Json.
#   VERSION     Package version. Defaults to 13.0.3.

set -euo pipefail

PACKAGE_ID="${1:-Newtonsoft.Json}"
VERSION="${2:-13.0.3}"

if [[ -z "${CHAINGUARD_DOTNET_IDENTITY_ID:-}" || -z "${CHAINGUARD_DOTNET_TOKEN:-}" ]]; then
  echo "Error: CHAINGUARD_DOTNET_IDENTITY_ID and CHAINGUARD_DOTNET_TOKEN must be set." >&2
  echo "Export them from a Chainguard Libraries pull token (see README.md)." >&2
  exit 1
fi

echo "Restoring package: $PACKAGE_ID $VERSION"
echo "Pass a different package id and version as arguments, for example"
echo "  $0 Serilog 4.3.0"

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/chainguard-dotnet-restore.XXXXXX")"
package_folder="$(mktemp -d "${TMPDIR:-/tmp}/chainguard-dotnet-packages.XXXXXX")"
trap 'rm -rf "$work_dir" "$package_folder"' EXIT

# Chainguard-only feed; %CHAINGUARD_DOTNET_IDENTITY_ID% and %CHAINGUARD_DOTNET_TOKEN% are substituted
# from the environment at restore time, so the token is not written to disk.
cat > "$work_dir/nuget.config" <<'XML'
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
XML

export NUGET_PACKAGES="$package_folder"
cd "$work_dir"
dotnet new classlib --name probe --output probe >/dev/null

echo "Adding $PACKAGE_ID $VERSION and restoring through libraries.cgr.dev/dotnet ..."
dotnet add probe package "$PACKAGE_ID" --version "$VERSION"

echo
echo "Restore succeeded through Chainguard Libraries for .NET."
