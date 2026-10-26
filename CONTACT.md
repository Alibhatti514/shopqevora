# Contact Us — implementation and operation

## Customer experience

Open `/contact`, or use **Help & Support → Contact Us** in the hamburger menu or **Contact Us** in the footer. `/information/contact` remains a working alias. The page uses the existing forest/ivory tokens, typography, logo and navigation. It has a responsive form, clear demo notice, loading/failure states, focused accessible confirmation and native keyboard-operable FAQ disclosures.

Fields: full name (2–120 characters), email (up to 254), optional international phone (up to 32, including `+` and country code), one of five inquiry categories, optional order reference (up to 64), and plain-text message (10–4,000). Shared DataAnnotations/IValidatableObject validation runs in Blazor and the API. Existing libphonenumber metadata performs authoritative international-number validation/normalization on the server. Phone/email ownership is not verified. Order references provide context only and grant no access to an order.

**This is an unmonitored demonstration inbox. No email delivery integration is configured, no emails are sent, and no reply time, shipping or refund policy is promised.** Use fictional data for evaluation; avoid passwords, payment details or sensitive information. FAQ answers describe actual demo behavior, including guest checkout and the absence of real payments/fulfillment.

The contact-specific InputText/InputTextArea components validate on input. This avoids losing a submit click when correcting a field removes an on-blur error and moves the button. Other forms are unchanged.

## API and protection

- `GET /api/contact/challenge` issues a random form-session key and an ASP.NET Core Data Protection token bound to that key and server issue time. Tokens expire after 20 minutes. Responses use `Cache-Control: no-store`.
- Passing the existing `submissionKey` to that endpoint refreshes the signed token while retaining the retry key. No personal information appears in this URL. Refreshing preserves the entered message and duplicate protection.
- `POST /api/contact/inquiries` accepts the shared JSON contract for guests or signed-in customers. It validates lengths, required values, category, plain text, international phone, signed token and empty honeypot, then stores the inquiry through EF Core/MySQL.
- Default limits: **5 submit attempts per IP per 10 minutes**, **30 challenge requests per IP per minute**, no queue. HTTP 429 includes `Retry-After`. Existing global 65,536-byte request limits also apply. Limits count invalid attempts as well as valid submissions. These are local per-process limits, not a claim of comprehensive bot prevention; trusted proxy/shared-limit configuration would need review for a multi-instance deployment.
- `Website` is an off-screen, keyboard/screen-reader-excluded honeypot. Filled values are rejected; no false success is returned for rejected spam.
- SHA-256 session/request hashes and a unique index enforce idempotence, including simultaneous identical requests. First creation returns 201; unchanged retries return 200 with the same acknowledgement. Reusing a key for different contact data/account attribution returns 409. Starting over explicitly clears fields and obtains a new key.
- Receipts contain only a non-sequential `CQ-` reference and creation time. **No public/customer inbox, list or reference-lookup endpoint exists.** Guest/authenticated attempts to read inquiries receive 404/405. There is no admin dashboard or email sender.
- Account attribution comes only from validated authentication claims; a client-supplied user ID is ignored. No cart/order/customer record is edited. Personal contact data is held in form memory and MySQL, not added to browser storage.

Data Protection uses the framework's existing server key-ring facilities; no new user secret or paid provider is required. Production operators must secure/persist the key ring and configure appropriate hosting/proxy/retention controls. This demo does not claim a legally complete commercial privacy policy or monitored support operation.

## Additive database upgrade

SQL: `database/20261026_AddContactInquiries.sql`. It creates **ContactInquiries only**, with private contact fields, optional server-derived user ID, UTC creation time and unique reference/session-hash indexes. Existing Identity, product, cart, order, order-line, guest-checkout and profile tables are not altered. No foreign key or relationship changes are required. EnsureCreated is not used to upgrade existing databases.

Run from the project root on the local Mac/Linux installation:

```bash
dotnet run --project tools/ShopQevora.E2E -- upgrade-contact
```

The explicit operator command takes a MySQL advisory lock, checks the schema, makes a full private `mysqldump --single-transaction` backup and only then executes the reviewed CREATE TABLE script. Backup files are outside source control under `~/.local/share/ShopQevora/backups/` (owner-only directory/files). Credentials are loaded internally from the unchanged User Secrets and are not printed or added to command-line arguments. Backup failure stops the upgrade. Existing table row counts are checked before/after. Repeated execution audits the expected columns/unique indexes and makes no changes. Windows/remote database upgrades require a separately reviewed private backup and manual SQL application.

**Applied locally:** the private full backup succeeded; ContactInquiries was created; all seven existing table row counts were unchanged. Running the application does not silently apply this migration. On a new empty development database, the existing fresh-database EnsureCreated initialization includes the new entity.

## Source files

- `src/ShopQevora.Client/Pages/Contact.razor`: both routes, form, state/confirmation and FAQ.
- `src/ShopQevora.Client/ContactModels.cs`: shared contract linked into the API, with validation and receipt/challenge types.
- `src/ShopQevora.Client/Components/ContactTextInput.razor` and `ContactTextArea.razor`: contact-only live validation.
- `src/ShopQevora.Client/Components/MainLayout.razor`, `InformationPages.cs`, `ShopService.cs`, `wwwroot/css/site.css`, `wwwroot/index.html`: links, accurate notice/privacy copy, HTTP calls, scoped styles and CSS cache version.
- `src/ShopQevora.Api/ContactInquiries.cs`: entity, token service, validation/idempotence, schema-presence check and public write/challenge endpoints.
- `src/ShopQevora.Api/Program.cs`, `ShopDb.cs`, `ShopQevora.Api.csproj`: dependency/rate-limit registration, CORS-readable rejection responses, EF indexes and contract link.
- `tools/ShopQevora.E2E/ContactUpgrade.cs`: explicit backup/upgrade command; fixture cleanup in `Program.cs` now includes only run-owned inquiries and preserves baseline counts.
- `tests/ShopQevora.Tests/ContactTests.cs`: validation, storage, attribution, privacy, spam, retry, expiry and rate-limit tests.
- `e2e/contact.spec.ts`: real guest/account, failure/retry, concurrent-post, privacy, accessibility/link/FAQ and responsive browser checks. Existing menu tests reflect the functional Contact Us destination.

## Verification and manual review

The focused 15-test Contact Us suite passed in Chromium, Firefox and WebKit. The final complete regression also passed: **209 Playwright tests**, **77 xUnit tests**, **11 JavaScript tests**, TypeScript and restore/build (0 build warnings/errors). There were zero failed/skipped/flaky Playwright cases. Project counts: Chromium 55, Firefox 55, WebKit 55, Chromium touch 22, WebKit touch 22. Strict browser-error and axe checks passed. Full results are in VERIFICATION.md. Test-only permit limits are raised for shared-loopback browser concurrency; xUnit tests verify the actual default 5-attempt limit. MySQL fixtures use unique fictional email prefixes and guarded cleanup; existing records are not removed.

For manual review, restart the normal API/client, then open http://localhost:5173/contact. Submit fictional details as a guest and as a signed-in customer. Try invalid fields and an international phone; confirm optional fields can be blank, the success reference appears, no email is promised, and FAQ disclosures work. In DBeaver inspect ContactInquiries and confirm the original commerce/customer records remain. Do not publish private backup files or inquiry contents.

```bash
cd ~/Downloads/ShopQevora
bash scripts/start-api.sh
# In a second terminal:
cd ~/Downloads/ShopQevora
bash scripts/start-client.sh
```


## Screenshots and final artifacts

- [Desktop page](test-output/contact-review/contact-desktop.png)
- [Mobile page](test-output/contact-review/contact-mobile.png)
- [Filled form](test-output/contact-review/filled-form.png)
- [Accessible confirmation](test-output/contact-review/confirmation.png)

These are actual running-application captures, not mockups. The HTML report is `test-output/report/index.html` (`npm run test:e2e:report`); machine results are `test-output/results.json`. Generated screenshots/reports are ignored. Final cleanup removed only run-owned fictional accounts/orders/inquiries and restored original counts; existing records/catalog remained unchanged.
