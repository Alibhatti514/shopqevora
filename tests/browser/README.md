# Optional real-browser verification

These are the actual Chrome verification scripts used during the rebrand, with portable output paths. They are separate from the dependency-free Node unit tests. Playwright is a temporary QA tool, not an application/runtime dependency.

**verify.mjs creates one fictional guest order in the configured local MySQL database per run**, plus an idempotent retry. It also injects one expected 503 to check bag preservation. It never removes existing records. Do not run against production. Review DATABASE_UPGRADE.md and use a private backup before database work. The companion schema must exist.

Start the API on 5101 and client on 5174 for isolated QA. The scripts bridge the client's normal 5100 API requests to 5101; normal application configuration stays unchanged.

```bash
dotnet run --project src/ShopQevora.Api --urls http://localhost:5101
dotnet run --project src/ShopQevora.Client --urls http://localhost:5174
```

In a third terminal, with Google Chrome installed:

```bash
npm install --prefix /tmp/shopqevora-browser-qa playwright --no-audit --no-fund
PLAYWRIGHT_MODULE=file:///tmp/shopqevora-browser-qa/node_modules/playwright/index.mjs SHOPQEVORA_ALLOW_DEMO_ORDER=yes node tests/browser/verify.mjs
PLAYWRIGHT_MODULE=file:///tmp/shopqevora-browser-qa/node_modules/playwright/index.mjs node tests/browser/cart-and-logo.mjs
```

The second script tests local guest cart removal, six cart widths, menu Escape behavior and small-size logo rendering without placing an order. Screenshots and results are written under `output/browser-qa`. Contact details are fictional; the form/confirmation screenshots show no actual customer data. Reports include private demo order references; review artifacts before publishing. The one deliberately injected 503 console message is classified separately from unexpected errors.
