#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/ShopQevora.Api --launch-profile ShopQevora.Api
