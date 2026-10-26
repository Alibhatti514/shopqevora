# Real-browser end-to-end testing

Playwright Test + TypeScript runs the actual Blazor WebAssembly frontend and MySQL-backed ASP.NET API. Chromium, Firefox and WebKit binaries are installed by Playwright. No fake pages replace the application. Only the explicit temporary-failure scenario intercepts one guest POST with 503; all successful shopping/auth/order paths use the real API and database.

## Run

```bash
cd ~/Downloads/ShopQevora
npm ci
npx playwright install chromium firefox webkit
npx playwright test
npx playwright show-report test-output/report
```

Native MySQL must be running and the existing API User Secrets must be configured. Both additive companion schemas must already exist (guest: DATABASE_UPGRADE.md; profile: README.md). Playwright starts the actual API/client launch profiles via `e2e/start-server.mjs` on **5101/5174**, leaving normal **5100/5173** services untouched. It forwards browser API requests to the real test API and configures the corresponding CORS origin; it does not mock customer responses. Health checks verify ownership. If either test port is occupied, startup refuses to take it over. Only test-owned servers are shut down.

## Database isolation and credentials

The .NET helper under `tools/ShopQevora.E2E` reads the unchanged User Secrets store without printing values. It first attempts a dedicated `ShopQevora_E2E` database. The current application account lacks CREATE DATABASE privileges, so verified **run-scoped fixture mode** is used instead.

Each run has a random 128-bit ID. All disposable accounts and guest contacts have emails beginning `e2e-<run-id>-` and ending `@example.test`. Existing accounts are never used for authentication tests. Tests never edit products/prices or run reset/truncate/drop operations. Cleanup checks exact run ownership, user/email equality and guest order-owner/reference agreement before removing only that run's fixtures. Products are fingerprinted before/after; existing user/cart/order baseline counts are checked after cleanup. Existing valuable data and previous demo orders remain. Normal auto-increment gaps from creating/removing disposable orders are retained; identifiers/sequences are never reset.

Private runtime connection/signing configuration is stored in ignored `.e2e/runtime.json` with owner-only permissions; connection/signing values are erased from that file after successful cleanup. The test API uses an ephemeral signing key, not the actual application signing key. A health-instance ID prevents tests attaching to an unowned API. Overlapping runs are rejected; stale failed-run fixtures can be cleaned with:

```bash
dotnet run --project tools/ShopQevora.E2E -- cleanup
```

Cleanup refuses an active run owned by another process. Do not publish runtime files, traces, reports or screenshots without reviewing them; they can contain disposable fixture tokens/credentials and fictional contact details. No actual credentials are hard-coded into test sources.

The E2E API raises the configurable guest request limit to 1000/minute for its own isolated test process. The normal application default remains 10/minute. The test tool does not modify User Secrets, tracked connection settings, or the normal database name.

## Coverage

- Homepage structure, branding, SVG/favicons, five decoded hero photographs and unchanged catalog data.
- Carousel advance, arrows, pagination, pause/resume, hover/focus pause, reduced motion, CTA category targets and stable hero geometry.
- Section/card reveals, repeated scrolling both ways, SPA/history navigation and native timer/observer lifecycle diagnostics.
- Search, empty states, combined category/search, sorting, categories, labels and product-image decoding.
- Guest cart add/increase/decrease/remove/refresh and invalid quantities.
- Full guest form/order/confirmation, field validation, server price validation, invalid IDs/quantities, temporary failure preservation, duplicate/replayed/concurrent submissions, successful-only clearing and no public guest lookup.
- UI registration, password visibility, valid/invalid login, logout, authenticated cart CRUD/checkout/history, own-user boundaries, merge semantics and refresh persistence.
- axe-core WCAG 2 A/AA, 2.1 AA and 2.2 AA scans of homepage, login, registration, cart and checkout; skip link, focus, heading structure and menu keyboard controls.
- Console errors, unhandled exceptions, failed requests and unexpected HTTP 4xx/5xx are test failures. Expected validation/authorization/conflict/fault-injection responses are explicitly classified. Navigation cancellations are retained as diagnostics.

Product cards currently display details and an Add to bag control; there is no separate product-detail route. Tests verify those displayed details and the real single-product API rather than inventing a detail page.

## Responsive matrix

All fourteen sizes run in each desktop engine: 320×568, 375×667, 390×844, 430×932, 412×915, 600×960, 768×1024, 820×1180, 1024×1366, 1280×800, 1366×768, 1440×900, 1920×1080 and 2560×1440. Separate Chromium and WebKit iPhone touch projects exercise representative customer flows. Firefox mobile emulation is not claimed.

Screenshots use loaded local fonts, decoded visible images and reduced motion for stable captures. No defective pixel baselines are automatically accepted. Automated geometry checks and screenshot inspection complement, but do not replace, manual visual/accessibility review.

## Output and diagnostics

- HTML report: `test-output/report/index.html`
- Structured results: `test-output/results.json`
- Screenshots and failure traces/videos: `test-output/results/`
- Inspect a failure: `npx playwright show-trace <trace.zip>`

All output directories, dependencies and private runtime files are ignored. Re-run failures with `npx playwright test --last-failed`, then execute the full suite. Source assertions must not be removed or weakened to hide defects.

## Professional enhancement coverage

`enhancements.spec.ts` verifies persistent header Cart/quantity, drawer keyboard/overlay/focus lifecycle, Shop All resetting filters, every demo information route, combined price/category/search filters and ascending/descending sorts. `profiles.spec.ts` exercises real disposable-account profile/address edits, dialing regions, avatar decoding/removal, ownership boundaries, pending email requests, password checks/revocation and responsive account pages. Existing commerce and carousel suites are retained. Startup screenshots use real local assets and real framework responses held briefly in tests; the application has no artificial delay.

Firefox's `FontFaceSet.ready` can stay pending while a deferred framework download is deliberately held. The startup test explicitly loads/checks the two local brand fonts instead. The config sets Playwright's `PW_TEST_SCREENSHOT_NO_FONTS_READY` switch to avoid its duplicate pending-document wait; normal screenshot helpers still await fonts and visible-image decoding. This changes test capture behavior, not application animation timing or font delivery. No assertion was removed to conceal an application defect.

## Contact Us coverage

`e2e/contact.spec.ts` adds guest and signed-in submissions, native/shared validation, blank optional fields, international phones, real MySQL idempotent/concurrent retries, fault recovery with message/key preservation, spam/token/length rejection, private-read denial, Help & Support/footer/legacy links, FAQ keyboard access, axe scans and all 14 responsive dimensions. It runs in all three desktop engines and both touch projects. Contact-only test accounts/emails use the same private run markers; cleanup removes only owned inquiries and checks baseline inquiry counts as well as existing commerce/customer counts.

The normal application uses 5 contact attempts per IP per 10 minutes and 30 challenges per minute. Only owned E2E server instances raise those limits for concurrent tests; the default limiter remains covered by xUnit. No response, user-input, privacy or browser-error assertions are bypassed. The order refresh test explicitly waits for the removal acknowledgement/UI-ready state before reloading, avoiding a WebKit in-flight request cancellation race while keeping strict error auditing.
