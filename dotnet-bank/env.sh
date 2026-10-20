#!/bin/bash
if [ -z "$1" ]; then
  echo "Usage: source env.sh <chainguard-parent-org>" >&2
  return 1 2>/dev/null || exit 1
fi

eval "$(chainctl auth pull-token --repository=dotnet --ttl=8760h --parent=$1 -o env)"

if [ -n "$CHAINGUARD_DOTNET_IDENTITY_ID" ] && [ -n "$CHAINGUARD_DOTNET_TOKEN" ]; then
  echo "CHAINGUARD_DOTNET_IDENTITY_ID and CHAINGUARD_DOTNET_TOKEN successfully set"
else
  echo "Failed to set Chainguard .NET credentials" >&2
fi
