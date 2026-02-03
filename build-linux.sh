#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

SLNF="${SLNF:-env0.linux.slnf}"
CONFIG="${CONFIG:-Release}"

if [[ ! -f "$SLNF" ]]; then
  echo "Missing solution filter: $SLNF" >&2
  exit 2
fi

echo "==> dotnet clean ($SLNF)"
dotnet clean "$SLNF" -c "$CONFIG"

echo "==> dotnet build ($SLNF)"
dotnet build "$SLNF" -c "$CONFIG"

echo "==> dotnet test ($SLNF)"
dotnet test "$SLNF" -c "$CONFIG" --no-build
