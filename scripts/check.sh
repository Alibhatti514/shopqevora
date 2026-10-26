#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
echo '== Checking required tools =='
dotnet --version
echo '== Restore =='
dotnet restore ShopQevora.sln
echo '== Build =='
dotnet build ShopQevora.sln --no-restore
echo '== Unit tests =='
dotnet test tests/ShopQevora.Tests/ShopQevora.Tests.csproj --no-build --logger 'console;verbosity=normal'
echo '== All build and unit-test checks completed =='

if command -v node >/dev/null 2>&1; then
  node --test tests/ui/*.test.mjs
else
  echo 'Node unavailable: run the JavaScript tests separately with Node 20+.'
fi
