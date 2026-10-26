#!/usr/bin/env bash
set -euo pipefail
API=http://localhost:5100
curl -fsS "$API/api/health"; echo
curl -fsS "$API/api/products" | python3 -c 'import json,sys; p=json.load(sys.stdin); assert len(p)>=6; print("Products returned:",len(p))'
status=$(curl -s -o /dev/null -w '%{http_code}' "$API/api/cart")
[ "$status" = "401" ] || { echo "Expected 401 for unauthenticated cart, got $status"; exit 1; }
echo 'PASS: unauthorized cart returns HTTP 401'
