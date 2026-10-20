#!/usr/bin/env bash
#
# Downloads a single package archive from Chainguard Libraries for .NET with
# curl to confirm direct access to the feed at libraries.cgr.dev/dotnet.
#
# Credentials come from a Chainguard Libraries pull token. Export them first
# (see README.md and access-examples.md):
#
#   eval "$(chainctl auth pull-token --repository=dotnet -o env)"
#
# Usage: download-single-file-curl.sh [PACKAGE_ID] [VERSION]
#   PACKAGE_ID  NuGet package id. Defaults to Newtonsoft.Json.
#   VERSION     Package version. Defaults to 13.0.3.

set -euo pipefail

BASE_URL="https://libraries.cgr.dev/dotnet/v3/flatcontainer"
PACKAGE_ID="${1:-Newtonsoft.Json}"
VERSION="${2:-13.0.3}"

if [[ -z "${CHAINGUARD_DOTNET_IDENTITY_ID:-}" || -z "${CHAINGUARD_DOTNET_TOKEN:-}" ]]; then
  echo "Error: CHAINGUARD_DOTNET_IDENTITY_ID and CHAINGUARD_DOTNET_TOKEN must be set." >&2
  echo "Export them from a Chainguard Libraries pull token (see README.md)." >&2
  exit 1
fi

# The NuGet flat container addresses packages by lowercased id and version.
id_lower=$(printf '%s' "$PACKAGE_ID" | tr '[:upper:]' '[:lower:]')
version_lower=$(printf '%s' "$VERSION" | tr '[:upper:]' '[:lower:]')
archive="${id_lower}.${version_lower}.nupkg"
url="${BASE_URL}/${id_lower}/${version_lower}/${archive}"

echo "Downloading: $url"
echo "Writing to:  $archive"
echo "Pass a different package id and version as arguments, for example"
echo "  $0 FluentValidation 12.1.1"

# -L follows the redirect the feed issues to its backing object storage;
# without it curl saves the redirect response instead of the archive.
curl -f -S -L -X GET \
  "$url" \
  -u "$CHAINGUARD_DOTNET_IDENTITY_ID:$CHAINGUARD_DOTNET_TOKEN" \
  --output "$archive"

echo "Download successful. Archive saved to '$archive'."
