# ShopQevora verification — 26 October 2026

**Latest account/catalog enhancement results are at the end of this document. Earlier sections record previous implementation milestones.**

## Source, branding and preservation

The root/solution/three projects were renamed in place. The API UserSecretsId is unchanged. All 48 original responsive photography files were compared against pre-rebrand SHA256 values and are byte-for-byte unchanged. Product information, prices/categories/image mappings, palette, site fonts, header/footer, cards/grids and carousel photographs/timing are preserved.

Pure-vector main/light logos, main/light SQ symbols and favicon have viewBoxes/titles and no raster/external-font dependencies. SVG contracts and the unchanged secret identifier are tested. Remaining old-brand text is intentional compatibility, historical rebrand documentation or existing database configuration; no credentials are printed or renamed.

## Database

The explicit schema audit verified existing InnoDB Orders, signed int Id and no UserId foreign key. The reviewed additive upgrade created **GuestCheckouts only**, linked to existing Orders. No existing table was altered/dropped. Backup instructions were documented before applying it; an automatic backup was not performed. Subsequent audits validate the table without changing it.

Existing MySQL secrets/accounts/products/cart/order history remain. Browser QA added **three fictional guest demo orders** (each two Studio Headphones at $129, total $258). Their matching retries returned the same references without additional orders. Fictional contact information is labeled Portfolio QA Guest / example.test and retained for DBeaver inspection; existing customer data was not removed.

## Executed automated checks

Executed successfully:

- `dotnet restore ShopQevora.sln` — successful.
- `dotnet build ShopQevora.sln --no-restore` — successful, **0 warnings / 0 errors**.
- `dotnet test ShopQevora.sln --no-build --verbosity normal` — **55/55 passing**.
- `node --test tests/ui/*.test.mjs` — **8/8 passing**.

The suites cover:

- All original Product/Cart/filter/validation/image/carousel tests.
- Guest storage refresh, quantity bounds, removal, retry keys, successful-only clearing, failed/malformed-response preservation and no contact/password storage.
- Guest field/product/quantity validation, authoritative server prices, successful persistence, duplicate retry/conflict handling and existing account-order preservation.
- ASP.NET TestServer boundaries: anonymous checkout, protected cart/orders, no anonymous guest lookup, own-user filtering, rate limiting, old JWT compatibility, merge retry semantics, account registration/login/cart/checkout/history.
- Scroll initialization/remount cleanup, dynamic nodes, reverse direction, fully-exited reset, reduced motion and hero exclusion; original carousel timer/focus/disposal tests remain.

.NET tests use EF InMemory/TestServer only inside the test project; the app remains MySQL. Ignored fake-provider transaction warnings do not prove MySQL transaction concurrency. Real MySQL guest persistence/idempotence was exercised separately in Chrome QA.

## Actual automated Chrome checks

Headless installed Google Chrome was controlled through temporary Playwright tooling outside the repository. Frontend/API verification servers used 5174/5101 with a test-only request bridge; normal application ports remain 5173/5100. Runtime app dependencies did not change.

- Homepage, guest checkout and guest confirmation checked at **375, 430, 768, 1024, 1440 and 1920px**, with no horizontal overflow.
- Cart was also checked at all six widths; guest removal reaches its empty state. Mobile menu opens and Escape closes it/returns focus. Main/light SVG and 16px/32px favicon render previews were captured.
- Logo loads, 20 catalog products, full-width hero, autoplay advance after ~2200ms and reduced-motion pause verified.
- Reverse-scrolled heading reached `shown` with direction `up`.
- Guest add → refresh → quantity update → form → real MySQL order → confirmation → cleared bag verified.
- A deliberately injected 503 preserved the cart; retry succeeded. One expected failed-resource console message accompanied that injected failure; **zero unexpected page/console errors**.
- Same-key POST retry returned the existing order reference. Contact details were absent from local/session storage.
- Screenshots/results: `output/browser-qa`. Screenshots are actual browser output, not fabricated mockups.

Chrome QA found and corrected initial route-focus autoplay pausing and an over-clipped reveal target that could block its observer. Final checks passed after correction. Manual Chrome/DBeaver review is still recommended, especially keyboard/back-forward behavior, favicon at small sizes, real account merge across refresh/failure and preserved existing account orders. The authoritative Coursera rubric is absent, so exact rubric compliance remains unverified.

## Limits

Demo/no payment, no actual shipping, no email receipts or public guest-order lookup. Guest confirmation is memory-only and disappears on refresh. Pakistan is the only demo address country. Shared cart products use the larger quantity rather than sum. Local storage is required for guest persistence. Production needs HTTPS and operational/privacy/retention safeguards. This workspace has no `.git` repository; nothing was published or submitted.

## Part 9 — final comprehensive Playwright run

The final ordered verification completed on 26 October 2026: restore → build → xUnit → existing JavaScript tests → TypeScript → complete Playwright suite.

| Check | Actual result |
|---|---|
| .NET restore | Successful |
| .NET build | Successful; 0 warnings, 0 errors |
| xUnit | 55 passed |
| Existing JavaScript unit tests | 8 passed |
| TypeScript | Successful |
| Playwright | **128 passed, 0 failed, 0 skipped, 0 flaky** |
| Chromium | 36 passed |
| Firefox | 36 passed |
| WebKit | 36 passed |
| Chromium iPhone touch emulation | 10 passed |
| WebKit iPhone touch emulation | 10 passed |

All fourteen requested dimensions ran in each desktop engine: 320×568, 375×667, 390×844, 430×932, 412×915, 600×960, 768×1024, 820×1180, 1024×1366, 1280×800, 1366×768, 1440×900, 1920×1080, 2560×1440. Additional touch projects exercised customer and axe accessibility flows. No physical-device or Firefox mobile-emulation claim is made.

The actual API/frontend were automatically started on 5100/5173 using the startup scripts. A health ownership check prevented attaching to an unrelated API. MySQL was live. The app account could not create a dedicated database, so guarded run-scoped fixtures were used. Final cleanup removed only that run's 15 disposable users and 23 orders; existing customer/cart/order counts were restored and the product fingerprint was unchanged. The previous three fictional QA orders were not deleted. Private runtime connection/signing values were erased after cleanup, and test-owned servers shut down.

The suite verified registration/login/logout, authorized cart/checkout/history, guest cart persistence and CRUD, guest form/confirmation, server prices, bad IDs/quantities/details, API-failure preservation, duplicate/concurrent request protection, guest privacy, account merge, catalog search/filter/sort, all product images, carousel controls/autoplay/pause/focus/reduced motion, SPA timer/observer cleanup, repeated slow down/up scrolling, keyboard navigation and axe WCAG scans. No unexpected browser exceptions, console errors or HTTP failures remained in the final run. Deliberate 400/401/404/409/503 responses were classified as expected negative-test behavior.

### Defects found and corrected

- Small supporting copy on sage/sand failed contrast (4.33/4.43 rather than 4.5); it now uses the existing deep-green token.
- The Blazor-intercepted skip link did not reliably focus main content; it now does so explicitly.
- The only H1 disappeared from the accessibility tree after the first carousel slide; a stable screen-reader H1 now remains while visible slide headings retain their appearance.
- The main skip target used browser-default blue focus styling; its visible keyboard indicator now uses forest green.
- Test-harness issues were separately corrected: changing menu accessible names, actual Blazor validation-summary markup, lazy/srcset decode readiness, and waiting for the destination app to render before testing back navigation. Assertions were retained; no failures were hidden with skips, retries or blanket error suppression.

Final report: `test-output/report/index.html`; JSON: `test-output/results.json`; attached screenshots: `test-output/results/`. Failure traces/videos are retained only for failed runs. These directories are ignored. Passing screenshots at mobile, tablet, desktop and ultrawide sizes were visually inspected; no defective visual baseline was automatically accepted. See E2E_TESTING.md to reproduce the suite.

Remaining limits: automated axe/geometry checks do not replace manual screen-reader/physical-device review; no pixel-baseline comparison is configured. The application has no separate product-detail route, so card details and the single-product API were tested. Checkout remains a demo with no financial transaction. The authoritative Coursera rubric is still absent.

## Latest professional enhancement — final verified code

The final ordered commands all executed successfully:

| Command | Result |
|---|---|
| `dotnet restore ShopQevora.sln` | Passed |
| `dotnet build ShopQevora.sln --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test ShopQevora.sln --no-build` | **64 passed**, 0 failed, 0 skipped |
| `npm run test:js` | **11 passed**, 0 failed, 0 skipped |
| `npm run typecheck` | Passed |
| `npx playwright test` | **184 passed**, 0 failed, 0 skipped, 0 flaky; 599.68 seconds |

Browser breakdown: Chromium 50, Firefox 50, WebKit 50, Chromium touch 17, WebKit touch 17. The final run launched actual API/client instances on 5101/5174, with real local MySQL and guarded run-scoped fixtures. Normal 5100/5173 services were left untouched. Cleanup removed only 35 disposable users and 23 orders from that run, restored customer/cart/order baseline counts, verified the catalog fingerprint, erased private fixture connection/signing values, and stopped owned listeners.

All 14 viewport sizes ran in Chromium, Firefox and WebKit: **320×568, 375×667, 390×844, 430×932, 412×915, 600×960, 768×1024, 820×1180, 1024×1366, 1280×800, 1366×768, 1440×900, 1920×1080, 2560×1440**. Additional touch and account-page checks ran. No physical-device claim is made.

Actual coverage includes persistent Cart/label/count, navigation Escape/outside close/focus trap/restoration, all information links, Shop All resetting filters, profile/address editing/persistence/ownership, international phone normalization, image validation/removal, pending email requests, password checks and new-session revocation, combined search/category/price filters and all sorting directions, guest and authenticated checkout/cart/history, carousel/timer disposal, scroll reversal, reduced motion, startup/retry behavior and axe WCAG checks. Passed audit fixtures reported no unexpected application console errors, page exceptions, failed assets or unexpected HTTP failures. Expected negative validation/fault responses are explicitly recorded separately.

Startup captures hold the real framework request briefly, load/check the existing local brand fonts, capture the real initial placeholder and then resume into the real homepage. A Firefox/Playwright pending-document font wait was addressed in test capture infrastructure; no application delay or mocked homepage was introduced.

Image audit: 36 products, all 20 original records preserved exactly, 16 new original generated visualizations, 32 added responsive WebPs decoded locally (864,374 bytes combined), and successful browser catalog image decoding. Two explicit seed passes confirmed idempotence. Private full backup preceded the additive CustomerProfiles table; existing commerce/Identity tables were not altered.

Fixed source defects: nested-route stylesheet URL; WebKit focus restoration; nested filter-modal background inertness; named filter-chip group semantics; long email/name wrapping on mobile; Shop All reset behavior; malformed-request client status; and profile updates unnecessarily serializing read-only avatar/identity fields. A regression with a 200 KB avatar property verifies the update wire body remains small and excludes read-only fields.

Selected final screenshots are under `test-output/review/`; the HTML report is `test-output/report/index.html`, detailed machine results `test-output/results.json`. [ENHANCEMENT_REPORT.md](ENHANCEMENT_REPORT.md) links the screenshots and lists implementation files, constraints and exact startup commands.

Remaining manual work: user review in Chrome and DBeaver, physical-device review, external Coursera rubric comparison. Email requests remain pending because mail delivery is unconfigured; OAuth is deferred, and one default address is supported. Avatar inputs are limited to 40 KB/2048×2048. New stamped JWT sessions revoke on password change; compatible legacy tokens retain their original expiry. No real payment/shipping/fulfillment integration is claimed.

## Latest Contact Us verification — final complete run

| Command | Executed result |
|---|---|
| `dotnet restore ShopQevora.sln` | Passed |
| `dotnet build ShopQevora.sln --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test ShopQevora.sln --no-build` | **77 passed**, 0 failed, 0 skipped |
| `npm run test:js` | **11 passed**, 0 failed, 0 skipped |
| `npm run typecheck` | Passed |
| `npx playwright test` | **209 passed**, 0 failed, 0 skipped, 0 flaky; 659.06 seconds |

Engine/project counts: Chromium **55**, Firefox **55**, WebKit **55**, Chromium touch **22**, WebKit touch **22**. The 25 new contact browser cases cover all 14 existing dimensions (320×568 through 2560×1440) inside each project, both customer types, required/email/phone validation, successful real-MySQL storage and confirmation, blank optional fields, simultaneous duplicate POSTs, fault recovery without losing data/retry keys, spam/length/token rejection, private-read denial, menu/footer/legacy links, FAQ keyboard access and axe scans. All prior commerce/account/carousel/animation tests remain and pass. Strict unexpected HTTP, console and page-exception assertions remain enabled.

During testing, contact-only live inputs corrected a blur-validation layout shift that could move the submit button. The existing WebKit order test now confirms a completed removal and enabled checkout control before refreshing; its original immediate reload could cancel an in-flight cart request. That targeted case passed three consecutive retests, followed by the fully passing 209-test suite. No application commerce flow or browser-error assertion was removed/weakened.

The explicit backup-first ContactInquiries upgrade succeeded, with all seven preexisting table row counts unchanged. A repeated schema audit verified the 12 expected columns/three unique indexes and applied no further changes. Existing database/customer/product/cart/order data was not reset or altered by the migration.

The final E2E cleanup removed only that run's **45 disposable users, 23 orders and 20 inquiries**. Baseline customer/cart/order/inquiry counts were restored; the product fingerprint was unchanged; private fixture credentials were erased. Test-owned services stopped, leaving normal development processes untouched.

Actual desktop/mobile full-page captures were inspected. Selected contact screenshots: `test-output/contact-review/contact-desktop.png`, `contact-mobile.png`, `filled-form.png`, `confirmation.png`. HTML report: `test-output/report/index.html`; machine results: `test-output/results.json`. Outputs are ignored, not published. No physical-device or manual DBeaver verification is claimed.

Operational limitation: this is an explicitly unmonitored demo inbox with no email delivery or response-time promise. There is no public/customer inbox/lookup or admin UI. Local per-process rate limits require appropriate trusted-proxy/shared-limit planning for a multi-instance deployment. See CONTACT.md for limits, migration/source details and exact startup/manual-review steps.
