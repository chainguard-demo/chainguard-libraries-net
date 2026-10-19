#!/bin/bash
if [ -z "$1" ]; then
  echo "Usage: source env.sh <chainguard-parent-org>" >&2
  return 1 2>/dev/null || exit 1
fi

CG_PARENT_ORG=$1
unset NUGET_USERNAME NUGET_PASSWORD
eval "$(chainctl auth pull-token --repository=dotnet --ttl=8760h --parent=$CG_PARENT_ORG -o json | jq -r '"export NUGET_USERNAME=\(.identity_id)\nexport NUGET_PASSWORD=\(.token)"')"

if [ -n "$NUGET_USERNAME" ] && [ -n "$NUGET_PASSWORD" ]; then
  echo "NUGET_USERNAME and NUGET_PASSWORD successfully updated"
else
  echo "Failed to set NUGET_USERNAME and NUGET_PASSWORD" >&2
fi
