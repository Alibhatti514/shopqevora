# ShopQevora

A premium .NET 8 Blazor WebAssembly e-commerce capstone backed by ASP.NET Core, MySQL, Identity and JWT. ShopQevora is the comprehensive rebrand of the former ShopEase project. The existing forest-green/ivory theme, typography, logo, footer, five hero photographs and two-second autoplay are preserved. The original twenty products, prices, categories and photographs remain intact; the catalog now has 36 products, with accessible navigation, profile management and combined filtering.

**Demo checkout only: no payment, cash collection, shipping or fulfillment.** No card numbers or payment credentials are collected.

Repository slug for a future hosted repository: `shopqevora`. Source code is published on GitHub under `Alibhatti514/shopqevora`. The project is available for review and local setup.

## Run locally

Prerequisites: .NET SDK 8 (global.json selects the 8.0.400 feature band or later within .NET 8), native MySQL 8.4 on `127.0.0.1:3306`, database `Shop`, existing application account `shopease_app`, and the existing API User Secrets. Node 20+ is useful for JavaScript tests. Docker is not required; the old compose artifact is unused and was not used for this work.

The root was atomically renamed from `~/Downloads/ShopEase` to `~/Downloads/ShopQevora`. Stop older API/client processes with Ctrl+C and restart terminals/editors in the new directory. Do not create another nested copy.

Terminal 1:

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-api.sh
```

Terminal 2:

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-client.sh
```

Frontend: **http://localhost:5173**. API: **http://localhost:5100**. Hard-refresh after restarting. Old processes will continue serving old assemblies until stopped.

## Configuration and MySQL safety

The API's **UserSecretsId is unchanged**. Existing connection string and signing key remain in the same secret store; no new secrets are required. Required private keys are `ConnectionStrings:Shop` and `Jwt:Key` (32+ bytes). Do not print `user-secrets list`, paste credentials, replace the database, or commit secret values. Environment overrides take precedence; remove stale overrides privately if needed.

The database remains `Shop`; the existing database application username is intentionally unchanged. Do not rename tables or Identity records as part of the brand change. Product IDs, names, prices, categories, descriptions and image mappings remain intact. Development demo seeding remains additive/idempotent.

Guest checkout adds one companion table, `GuestCheckouts`, linked to the existing Orders table. Read the local project documentation for the private backup procedure, schema checks, reviewed SQL, and explicit additive command:

```bash
dotnet run --project src/ShopQevora.Api -- --audit-guest-schema
dotnet run --project src/ShopQevora.Api -- --apply-guest-schema
```

The local upgrade was applied during implementation. Repeating it validates the table without altering existing data. Other existing installations need this upgrade. `EnsureCreatedAsync` does not migrate an existing database; guest checkout returns 503 if the companion table is missing. No existing table is altered/dropped. Authenticated shopping remains available.

## Shopping and checkout

- Existing catalog, search/category filters, price/name/newest sorting, counts and empty/loading states.
- Existing premium product cards and responsive image sources.
- Guest bag: add, update quantities 1–99, remove, display and total; persists across refresh in `localStorage`.
- Browser storage contains only product IDs, quantities and a random private retry key. Prices are loaded from the API; contact/address details are never placed in application browser storage.
- Checkout offers **Continue as Guest**, **Sign In**, and **Create Account**.
- Guest form: name, email, phone, address, city, province/region, optional postal code. Pakistan is the only demo country currently supported. No shipping service is claimed.
- Server validates all lines/contact fields, rejects invalid/unknown products, reads MySQL prices and computes totals. Client prices/totals are ignored.
- Guest orders persist in existing Orders/OrderLines plus GuestCheckouts. Confirmation includes a random reference, purchased lines, authoritative total, delivery summary and an explicit no-payment/no-fulfillment status.
- Successful checkout clears the bag; failures preserve it. Same-key retries with the same normalized request return the existing order; changed requests with the same key receive 409.
- Guest confirmation is held only in memory. Save the reference before leaving; refresh removes the displayed contact details. No public order lookup or email receipt feature is provided.
- Signing in merges guest items without duplicate rows: overlapping products keep the larger quantity, capped at 99. This idempotent maximum strategy never reduces either saved selection and avoids double-adds after retries. A failed merge preserves the local bag and exposes a retry control in the account bag.
- Authenticated carts still persist in MySQL; registration, login, protected checkout, own-order history and all required Cart methods remain.

## Identity and privacy

New JWTs use ShopQevora issuer/audience. Pre-rebrand signed JWT issuer/audience and old sessionStorage keys are intentionally accepted/migrated for compatibility; passwords are never stored in browser storage. Existing User Secrets, password hashes and Identity rows were not replaced.

Guest POST `/api/guest-orders` is public but validates requests, uses authoritative prices, bounds quantities/line counts, limits request bodies, and rate-limits by IP (10/minute). No anonymous GET exposes orders or contacts. A hashed random retry key and fingerprint protect replays; unique MySQL indexes prevent duplicate saves. Authenticated `/api/orders` remains scoped by authenticated user ID; guest owners use a separate opaque `guest:` namespace. New order/reference values are not predictable sequential public lookup credentials.

This is a local demo, not production infrastructure. Production needs HTTPS, backup/retention policies, hardened session handling, email verification, operational abuse controls and real shipping/payment integrations. Guest contact data in MySQL/backups must be treated as private; use fictional details for demonstrations.

## Branding and motion

- Local SVG wordmarks: `wwwroot/logo.svg`, `logo-light.svg`.
- Intertwined SQ symbols: `wwwroot/monogram.svg`, `monogram-light.svg`.
- Matching small-size favicon: `wwwroot/favicon.svg`.
- The custom treatment uses an interwoven S/Q ribbon, carefully tightened outlined lettering and restrained offset vector layers/highlights. It is 3D-inspired vector artwork, not a claimed 3D model. No external fonts or raster images are embedded in the logo.
- Existing Manrope/Fraunces site typography and color tokens remain.
- `scroll-motion.js` progressively enhances existing section/category/editorial/footer headings, cards and images via IntersectionObserver. Selected short text uses Blazor-rendered word/character spans with full screen-reader text.
- Reveals reset only after fully leaving the viewport, then replay with the new scroll direction. Partially visible text is not erased. Short staggered captions complete quickly; essential prices/checkout instructions are not typed.
- Default content is visible when JS fails. Reduced motion displays all content without transitions. MutationObserver handles Blazor route/data updates; disposal removes observers, listeners and pending animation frames. Hero slides are excluded and keep their existing timing.
- Router focus now goes to the main content container, so programmatic route focus does not pause the hero on initial navigation. Keyboard focus inside carousel controls still pauses it.

## Architecture

`ShopQevora.Client` → HTTP JSON API → `ShopQevora.Api` → EF Core/Pomelo MySQL `Shop`.

- .NET/ASP.NET Core/Blazor 8; EF Core/Identity stores 8.0.11; Pomelo 8.0.2; xUnit.
- Models preserve ProductID, Name, Price, Category, Description and ImageUrl.
- Cart preserves AddProduct, RemoveProduct, DisplayCartItems, CalculateTotal.
- Existing Products, CartItems, Orders, OrderLines and ASP.NET Core Identity tables are unchanged.
- CustomerProfiles stores one private profile/default address, normalized phone, pending email and sanitized avatar per Identity user.
- GuestCheckouts stores OrderId, random Reference, CheckoutKeyHash, RequestHash and validated ContactJson. It references Orders without changing its schema. Status is fixed to the explicitly modeled demo semantics.
- Guest order/line/companion inserts use one SaveChanges transaction; authenticated checkout retains its serializable transaction.
- `GuestCartStore` / `GuestCartState`: local non-sensitive guest bag and retry key.
- `ShopService`: guest/account cart routing, merge, session compatibility and checkout.
- `GuestOrderService`: server validation, price calculation, order persistence and idempotent replay.
- `GuestCheckoutSchema`: explicitly invoked additive schema audit/upgrade.

## Automated checks

```bash
dotnet restore ShopQevora.sln
dotnet build ShopQevora.sln --no-restore
dotnet test ShopQevora.sln --no-build --verbosity normal
node --test tests/ui/*.test.mjs
bash scripts/smoke.sh
```

The .NET and Node suites do not require MySQL: EF InMemory and an ASP.NET TestServer are used only inside tests. The application continues using MySQL. TestServer transaction warnings are suppressed for the fake provider; these tests do not establish real MySQL concurrency/transaction behavior. Smoke checks need the API/MySQL. Test credentials/signing material are generated at runtime and do not replace application secrets.

Coverage includes original commerce behavior, guest storage/quantities/removal, success/failure cart clearing, required-field/product/quantity validation, server prices, retry conflicts, anonymous-order privacy, protected endpoints, old JWT compatibility, account registration/login/checkout/history, merge retries and rate limiting. JavaScript tests cover carousel lifecycle plus scroll initialization, cleanup, remounting, reverse scrolling, reduced motion and hero exclusion. Current Playwright screenshots and the HTML report are generated under ignored `test-output/`; reproducible TypeScript tooling is in the repository. Browser automation runs actual API/client instances on 5101/5174 while normal startup remains 5100/5173. Earlier Chrome smoke artifacts remain under `output/browser-qa` and `tests/browser` for historical reference.

## Manual review and screenshots

- [ ] Review logo clarity in header/footer and favicon at 16/32px.
- [ ] Check 375, 430, 768, 1024, 1440 and 1920px, mobile menu, full hero, product cards and footer.
- [ ] Scroll both directions; test reduced motion, keyboard focus and back/forward navigation.
- [ ] Add guest products, refresh, update/remove, check totals and clear/empty states.
- [ ] Place a fictional guest demo order; compare MySQL prices, reference, lines, contact summary and no-payment status.
- [ ] Test failure/retry, successful bag clearing and privacy after refresh.
- [ ] Sign in with guest items; verify maximum-quantity merge and retry on failure.
- [ ] Register/login, account cart, authorized checkout and own-order history.
- [ ] In DBeaver inspect preserved records and new GuestCheckouts rows. Keep backups/screenshots private.

## Troubleshooting and submission

Port already used: stop the older processes before starting the renamed projects. Missing secrets: use the API's unchanged UserSecretsId privately. Missing guest schema: follow the local project documentation rather than resetting MySQL. Unavailable database: check native service, port 3306 and existing account grants. Expired sessions: sign in again. Guest reference/confirmation unavailable after refresh: intentional memory-only UI; no public lookup exists. Browser storage disabled: guest persistence cannot work reliably; the UI reports errors.

Product visuals are illustrative demo assets, including AI-generated images. The source code is published on GitHub. Coursera submission must be completed separately, following the assignment requirements.

## Comprehensive Playwright Test suite

Part 9 adds `playwright.config.ts`, TypeScript specifications under `e2e`, axe-core, three browser engines, fourteen viewport sizes and representative touch projects. The suite starts separate real application instances at 5101/5174 and uses safely run-scoped MySQL fixtures with guarded cleanup. No existing customer data is used for test logins or deleted during cleanup.

## Professional account and catalog enhancement (26 October 2026)

The existing forest/ivory storefront, SQ logo, photographs, carousel, motion system and commerce flows are retained. The top bar now keeps **Cart**, its icon and item count visible at every size. Account links live in an accessible navigation drawer with Escape/outside close, focus trapping/restoration, background inertness, scroll locking and reduced-motion support.

- `/account`: authenticated profile and default address overview.
- `/account/edit`: full/display names, international phone dialing region, normalized E.164 number, default shipping address and avatar preview/upload/removal.
- `/account/settings`: current-password-checked password changes and pending email-change requests. No email is sent or marked verified: no email delivery service is configured. Newly issued JWTs contain an Identity security stamp and are invalidated by password changes. Legacy compatible tokens retain their original maximum eight-hour lifetime. Signing out clears this browser's session.
- `/information/{about,contact,shipping,returns,privacy,terms}`: explicitly identified educational/demo information, without invented commercial policies or contact details.
- The catalog contains **36 demo products**, including 16 original AI-generated product visualizations. Local 800×1000 and 400×500 WebPs use the existing card layout. Original 20 product records, IDs and prices are preserved.
- Search, category, minimum/maximum price and sorting combine. Filters show active counts and removable chips. The filter drawer updates results immediately; Show results closes it. Sort options: curated catalog order, newest/oldest catalog additions, price ascending/descending and name A–Z/Z–A. No sales/popularity claims or inventory filters are fabricated.

### Profile database upgrade

Existing `EnsureCreated` databases require the additive upgrade below; startup does **not** migrate the schema. The local operator helper inspects Identity key collation and backs up the entire database with `mysqldump --single-transaction` to a private owner-only directory outside the repository before adding `CustomerProfiles`. Credentials are loaded internally from the unchanged User Secrets configuration; never paste them into shell commands.

```bash
cd ~/Downloads/ShopQevora
dotnet run --project tools/ShopQevora.E2E -- upgrade-profiles
```

This upgrade was applied locally. Backups are under `~/.local/share/ShopQevora/backups/` (contain private data; never commit or share). Review [database/20261026_AddCustomerProfiles.sql](database/20261026_AddCustomerProfiles.sql). It adds one table, with a foreign key to the existing Identity user; no existing tables are altered. `CREATE TABLE IF NOT EXISTS` is repeatable. The helper refuses unexpected Identity collation and stops if backup fails. On a different installation, review the target schema and backup first. `mysqldump` must be on PATH; Windows requires a separately reviewed private backup procedure.

Optional explicit development catalog insertion (idempotent; no original data replacement):

```bash
dotnet run --project tools/ShopQevora.E2E -- seed-catalog
```

### Avatar and phone handling

Avatars accept PNG/JPEG/WebP up to **40 KB** and **2048×2048**. The API verifies encoded format, decodes complete pixels with SkiaSharp, center-crops to 160×160 and re-encodes a metadata-free PNG. No supplied filename or external URL is stored. Avatar bytes are private profile data, returned only through the authorized owner's endpoint. Resize larger photos before upload. International region/calling-code metadata and validation come from libphonenumber-csharp; validation does **not** verify ownership. Skia native runtime assets are included for macOS, Windows and Linux.

The profile holds one default address. Multiple saved addresses and real email delivery/OAuth remain future enhancements. There are no fake social-login buttons. Profile addresses are not silently copied into or otherwise used to change the existing checkout flow.

### Verification and development ports

Normal startup stays on API **5100** and frontend **5173**:

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-api.sh
# In a second terminal:
cd ~/Downloads/ShopQevora
bash scripts/start-client.sh
```

Restart an already running API/client after rebuilding. Playwright uses separate **5101/5174** listeners so normal development servers remain untouched. Its browser request routing forwards requests to the real test API (not mocked responses), with a matching CORS origin. Fixture ownership and catalog fingerprint checks still apply. Test cleanup removes only run-scoped fictional accounts/orders and their cascaded profiles. Reports/screenshots are ignored in `test-output/`.

```bash
dotnet restore ShopQevora.sln
dotnet build ShopQevora.sln --no-restore
dotnet test ShopQevora.sln --no-build
npm run test:js
npm run typecheck
npx playwright test
```

Final enhancement verification: **64 xUnit tests, 11 JavaScript tests and 184 Playwright tests passed**, with zero failures/skips; TypeScript and restore/build passed (0 build warnings/errors). Chromium, Firefox, WebKit and representative touch contexts exercised 14 requested dimensions. The full Playwright report is generated locally under `test-output/report/`.

## Contact Us

A functional, responsive Contact Us page is available at `/contact`, **Help & Support → Contact Us**, and the footer. The previous `/information/contact` URL remains valid. Guests and signed-in customers can submit validated inquiries with optional international phone/order reference. Inquiries are stored privately in MySQL; signed form tokens, a honeypot, input/body limits, per-IP rate limiting and idempotent retries protect the endpoint. There is no public inquiry lookup.

**Demo inbox: not actively monitored; no emails are sent or response time promised.** FAQs describe actual demo checkout/shipping/refund behavior without invented business policies.

Existing databases require the explicit backed-up additive upgrade (already applied locally):

```bash
dotnet run --project tools/ShopQevora.E2E -- upgrade-contact
```

This creates only `ContactInquiries`, after a private full backup; existing customer/product/cart/order records remain unchanged. It is not an EnsureCreated migration. Restart existing API/client processes to load the new routes and hard-refresh the frontend. Normal ports stay 5100/5173.

Contact Us final verification: **77 xUnit tests, 11 JavaScript tests and 209 Playwright tests passed**, with zero failures/skips; TypeScript and restore/build passed (0 build warnings/errors). Chromium, Firefox, WebKit and both touch projects passed, including the new page at all 14 responsive sizes.
