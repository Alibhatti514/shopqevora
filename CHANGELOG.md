# ShopQevora enhancement and rebrand — 26 October 2026

## Preserved

The existing forest/ivory/cream palette, site fonts, header/footer layout, hero structure and 2000ms timing, all 48 photography files, twenty product names/prices/categories/image mappings, product cards/grids and editorial/category structure are preserved. No framework rewrite or payment gateway was introduced.

## Renamed

- Root: `~/Downloads/ShopEase` → `~/Downloads/ShopQevora` (atomic move, not a second nested copy).
- Solution: `ShopEase.sln` → `ShopQevora.sln`.
- Projects/folders: `src/ShopQevora.Api`, `src/ShopQevora.Client`, `tests/ShopQevora.Tests`, each with matching `.csproj`.
- C# namespace, RootNamespace and AssemblyName prefixes: `ShopQevora`.
- Solution/project references, Razor imports, launch profiles, scripts, JS test paths, page titles, accessible brand labels and documentation now use the new name.
- Repository slug documented as `shopqevora`; there is no local Git repository/remote to rename or publish.
- Intentional exceptions: database `Shop`, existing database username/configuration, preserved UserSecretsId, compatibility JWT issuer/audience/session-storage keys, historical rebrand explanations and compatibility tests. No secrets or third-party copyright notices were blindly rewritten.

## Created

- API: `AuthSigning.cs`, `GuestCheckout.cs`, `GuestCheckoutSchema.cs`.
- Client: `GuestCartStore.cs`, `Components/RevealText.razor`, `Pages/Checkout.razor`, `Pages/GuestConfirmation.razor`, `wwwroot/js/scroll-motion.js`.
- Branding: original main/light SVG wordmarks, `monogram.svg`, `monogram-light.svg`, matching simplified favicon.
- Database: `database/20261026_AddGuestCheckouts.sql`, `DATABASE_UPGRADE.md`.
- Tests: `GuestCartTests.cs`, `GuestOrderTests.cs`, `GuestApiTests.cs`, `RebrandTests.cs`, `tests/ui/scroll-motion.test.mjs`, optional reproducible Chrome scripts under `tests/browser/`.
- Documentation: `BRAND_AND_MOTION.md` and updated verification/audit records.
- Review artifacts: photography SHA256 manifest and actual automated Chrome screenshots/results under `output/browser-qa`.

## Modified

- All source namespace/import/project identifiers and documentation paths.
- API `Program.cs`: deferred secret-backed signing configuration, new/legacy JWT compatibility, public validated/rate-limited guest POST, protected idempotent maximum-quantity cart merge, bounded bodies, safe error responses, explicit schema CLI.
- API `ShopDb.cs`: one companion-table mapping, without changing existing table/column names.
- API `.csproj`: unchanged UserSecretsId; reviewed upgrade SQL copied to build/publish outputs.
- Client `ShopService.cs`/`Models.cs`: guest/account routing, local bag persistence, session migration, merge/retry, guest confirmation and safe clearing rules.
- Client `Pages/CartPage.razor`: guest cart and checkout entry, unavailable-item protection, merge retry. Existing quantity/line-total/account checkout behavior remains.
- Client `Pages/Home.razor`: guest adds no longer redirect to sign-in; selected short text uses RevealText while retaining existing layout and copy.
- Client `Components/MainLayout.razor`: new identity references, layout-scoped motion initialization/disposal, main focus target; same navigation/footer structure.
- Client `App.razor`: route focus targets main so initial hero focus does not pause autoplay.
- Client `Components/ProductCard.razor`: the decorative SE mark becomes SQ; all photography/card styling is preserved.
- Client `wwwroot/css/site.css`: appended guest-form/motion rules; existing design-token/layout rules preserved.
- Client `wwwroot/index.html`: ShopQevora title/description/Open Graph and cache-busted brand assets.
- Scripts and test project references: renamed paths, optional JS test execution in check.sh.

## Data safety and verification

Only GuestCheckouts was added after physical MySQL schema audit and documented private backup instructions. No automatic backup was claimed. Existing tables/rows, Identity/password hashes, secrets, product IDs and authenticated orders were not reset or recreated. Real browser QA inserted explicitly fictional guest demo orders and left them for inspection; no cleanup deleted customer data. Read VERIFICATION.md for exact counts and limitations.

## Part 9 — comprehensive Playwright verification

- Added Playwright Test/TypeScript configuration, Chromium/Firefox/WebKit and representative touch projects, fourteen responsive dimensions, axe-core scans, real application startup and HTML/JSON reports.
- Added `e2e` specifications for homepage/carousel, navigation, catalog, guest/account cart, guest checkout, authentication, orders, scroll lifecycle, responsive layouts and accessibility.
- Added guarded `tools/ShopQevora.E2E` MySQL run-scoped fixture setup/cleanup, ownership/health checks and catalog/baseline preservation checks. Private runtime values and reports are ignored; credentials are never printed.
- Fixed supporting-text contrast on sage/sand using the existing deep-green token.
- Fixed the skip link to focus the main content reliably in Blazor navigation; its keyboard target indicator uses forest green instead of the browser-default blue.
- Kept one screen-reader heading available throughout carousel slide changes, without changing visible hero typography/layout.
- Corrected test selectors for changing menu labels/Blazor validation markup, and strengthened cross-engine image/readiness waits without dropping assertions.
- Added E2E_TESTING.md and ignored screenshot/report output. Theme, photography, products, prices and customer records remain unchanged.

## 2026-10-26 — Account, navigation and catalog enhancements

- Kept the existing theme, logo, hero photographs/timing, cards, editorial sections and checkout flows.
- Added persistent header Cart and an accessible account/navigation drawer; added honest demo information pages.
- Added authenticated profile/default address editing, international phone normalization, safe private avatar upload/removal, pending email requests and current-password-verified password changes. Security-stamp validation revokes newly issued sessions on password changes.
- Backed up local MySQL privately and added `CustomerProfiles` through an explicit additive upgrade.
- Added 16 original local product image pairs and idempotent seed records, retaining all original products.
- Added price filters, active chips, responsive filter drawer and descending-name/oldest-catalog sorting.
- Expanded account, navigation, filter, upload, ownership and lifecycle tests. Browser automation uses separate local ports to preserve running development services.
- Social sign-in is deferred: no OAuth provider credentials or callbacks are configured.

## 2026-10-26 — Functional Contact Us

- Added a branded responsive contact form for guests/accounts, Help & Support/footer links and the working legacy information URL alias.
- Added shared client/server validation, international phone normalization, accessible saved-reference confirmation, recovery states and truthful demo FAQ/monitoring notices.
- Added private MySQL ContactInquiries storage through an explicit full-backup-first additive SQL upgrade; original tables/records preserved.
- Added purpose-bound 20-minute Data Protection form tokens, honeypot rejection, input/body limits, 5-per-10-minute submission rate limiting, no-store receipts and indexed duplicate/concurrent retry protection. No public inbox/lookup or email-delivery claims.
- Added contact-specific input validation that clears corrected-field errors before a submit click, preventing blur-driven button movement.
- Added validation, successful persistence, attribution/privacy, spam/rate/expiry/idempotence tests and real multi-engine/touch/responsive Playwright coverage. Existing test fixtures now clean up only run-owned inquiries.
