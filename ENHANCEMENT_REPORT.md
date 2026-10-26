# ShopQevora professional enhancement

The existing visual system, SQ identity, hero carousel/photos/timing, original product photography, editorial content, scroll system and footer are retained. No project or database was renamed in this enhancement.

## Delivered behavior

- Header: logo, Shop, Categories, Search, permanently visible Cart/icon/count and hamburger. Sign In/account actions are inside the drawer.
- Drawer: Shopping (Shop All, New Arrivals, Collections/Categories, clearly curated Favorites/Best Sellers); signed-out or signed-in Account options; six accurate demo information pages. Keyboard trap, explicit opener restoration (including WebKit), inert background, scroll lock, Escape/outside close, route cleanup and reduced motion.
- Account: profile/default address overview; full/display name and address edits; international calling-region selector and E.164 phone normalization; private validated PNG/JPEG/WebP photo preview/save/removal; initials fallback; password changes; pending email requests. New sessions are invalidated after password changes. Phone ownership is not verified. No email is sent or silently marked verified.
- Catalog: 36 products; all original 20 records preserved exactly. Sixteen new original AI-generated visuals, each with two locally stored WebP sizes. Two seed passes confirmed idempotence. All 32 added files decoded locally; combined size 864,374 bytes. Existing local originals were not edited.
- Filters: live search/category/minimum/maximum price, removable chips, active count, Clear All, filter drawer and live results. Curated order, price ascending/descending, name ascending/descending and newest/oldest catalog order. No invented popularity, stock or sales rankings.
- Information: About, Contact, Shipping, Returns, Privacy and Terms explicitly describe an educational demo without fabricated support details or commercial promises.

## Database and security

A full private local `mysqldump` backup was created outside the repository before adding only `CustomerProfiles`. Existing Identity, product, cart, order, order-line and guest-order rows/tables were preserved. The new table references the existing Identity key with matching collation. Upgrade command and SQL are documented in README and DATABASE_UPGRADE. EnsureCreated is not used as an upgrade mechanism. User Secrets identifiers and values were not changed or displayed.

Profile ownership comes exclusively from authenticated claims; no target customer ID is accepted. Profile inputs enforce lengths, valid regions/phone formats and plain text. Avatars enforce request/file/dimension limits, actual encoded format and successful pixel decoding, then center-crop/re-encode to metadata-free PNG before storage. Arbitrary filenames/URLs/HTML/executable uploads are not accepted. Passwords remain under ASP.NET Core Identity. Legacy JWT compatibility remains bounded by original expiry.

Browser fixtures run on separate 5101/5174 instances. Disposable accounts/orders are run-marked, with guarded cleanup; the application account cannot create a dedicated database, so safe isolated fixtures use the local Shop database. Existing customers are never used for test sign-ins. Cleanup checks product fingerprints and original customer/cart/order counts.

## Main files

- UI: `Components/MainLayout.razor`, `Pages/Home.razor`, new `Pages/Account.razor`, new `Pages/Information.razor`, `Pages/Orders.razor`, `AccountModels.cs`, `InformationPages.cs`, `ProductPresentation.cs`, `ShopService.cs`, `wwwroot/js/drawer.js`, scoped additions to `wwwroot/css/site.css`, corrected absolute stylesheet URL in `wwwroot/index.html` (all under `src/ShopQevora.Client`).
- API: new `CustomerProfile.cs`; `Program.cs`, `ShopDb.cs`, `DemoCatalog.cs`, package references in `ShopQevora.Api.csproj`.
- Schema/operations: `database/20261026_AddCustomerProfiles.sql`; explicit backup/upgrade/seed commands in `tools/ShopQevora.E2E/Program.cs`.
- Assets: 32 new WebPs under `src/ShopQevora.Client/wwwroot/images/products`; exact manifest in `output/catalog-expansion.json`; original images untouched.
- Tests: new `ProfileTests.cs` and drawer lifecycle JS tests; updated catalog contract assertions; new `e2e/profiles.spec.ts` / `e2e/enhancements.spec.ts`; existing E2E specs updated for expanded data/navigation; `playwright.config.ts`, `e2e/helpers.ts`, `e2e/start-server.mjs`; small test-only avatar fixture.
- Documentation: README, CHANGELOG, IMAGE_AUDIT, ASSET_PROVENANCE, DATABASE_UPGRADE, E2E_TESTING, VERIFICATION and this report.

## Limitations and manual review

One default address is supported; multiple-address books remain optional future work. Real email delivery/verification and OAuth providers are not configured. There are no fake OAuth buttons, password-reset emails, real payments, deliveries or tracking claims. Avatar uploads must be resized below 40 KB before selection. Original legacy JWTs retain their existing expiry; new stamped sessions revoke after password changes. Physical devices and other operating systems were not manually tested.

After restarting the normal servers, review the menu, 320px header/cart, profile photo and pending-email notices in Chrome; inspect CustomerProfiles and preserved commerce records in DBeaver; compare photos/colors with your preferred baseline. Existing coursework methods/models and flows remain; external Coursera rubric review remains the owner's submission check.

## Startup

Stop the old API/client in their own terminals, then:

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-api.sh
```

In another terminal:

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-client.sh
```

Open http://localhost:5173 and hard-refresh. API is http://localhost:5100.

## Executed verification

Final ordered run: restore → build → xUnit → JavaScript → TypeScript → complete Playwright suite.

| Check | Actual result |
|---|---|
| Restore | Passed |
| Build | Passed, 0 warnings / 0 errors |
| xUnit | 64 passed, 0 failed / skipped |
| JavaScript | 11 passed, 0 failed / skipped |
| TypeScript | Passed |
| Playwright | 184 passed, 0 failed / skipped / flaky; 599.68 seconds |

Projects: Chromium 50, Firefox 50, WebKit 50, Chromium touch 17, WebKit touch 17. All fourteen requested viewport dimensions ran in each desktop engine, from 320×568 to 2560×1440, with additional account-page and touch checks. Startup tests cover normal loading, real framework requests briefly held by the test, reduced motion and retry after failures. All catalog images decode and original records match the baseline. Axe scans and unexpected console/page/network-error assertions pass.

The final cleanup removed only 35 run-owned disposable users and 23 orders, restored original customer/cart/order counts, verified the catalog fingerprint and erased private runtime fixture credentials. Test-owned listeners stopped; existing normal development servers were not terminated.

Screenshots from the final passing run:

- [Mobile menu](test-output/review/menu-mobile.png)
- [Desktop menu](test-output/review/menu-desktop.png)
- [Profile dashboard](test-output/review/profile-dashboard.png)
- [Firefox profile at 320px](test-output/review/profile-mobile-firefox.png)
- [Loading screen](test-output/review/loading-mobile.png)
- [Homepage](test-output/review/homepage.png)

HTML report: `test-output/report/index.html`; machine results: `test-output/results.json`. View with `npm run test:e2e:report`. Generated output is ignored. Asset paths/prompts are in [the expansion manifest](output/catalog-expansion.json).

Fixed defects include nested-route CSS 404s, WebKit opener focus, nested modal background isolation, filter-chip ARIA semantics, long-email mobile wrapping, Shop All filter reset, malformed-request status handling, and unnecessary avatar/read-only fields in profile update payloads. The latter now has a large-avatar wire-payload regression test. Harness corrections preserve real-data assertions and use explicit ready/font checks rather than racing Blazor startup or a deliberately held deferred script.
