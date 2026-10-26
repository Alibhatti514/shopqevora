# Guest checkout: additive database upgrade

The existing database remains `Shop`. Identity, Products, CartItems, Orders and OrderLines keep their names and data. The API UserSecretsId is unchanged. The existing order UserId column has no Identity foreign key in the EF model; the upgrade command verifies the physical schema before proceeding. Guest orders use a `guest:`-prefixed opaque reference as their owner and are never returned by authenticated order-history queries.

The only schema addition is **GuestCheckouts**, a one-to-one companion table for an existing Order. It stores a random order reference, hashed client retry key, request fingerprint and validated contact JSON. There is no anonymous GET endpoint. Guest contact data is not stored in localStorage/sessionStorage. Payment is always demo/not charged; no real shipping or payment service is provided.

## Back up first

Use a private location outside this repository and the web root. One native MySQL example is:

```bash
umask 077
mysqldump --host=127.0.0.1 --port=3306 --user=shopease_app --password --single-transaction --no-tablespaces Shop > "$HOME/Shop-before-guest-checkout.sql"
```

Enter the password only at the private prompt. Never paste it into chat, source or command arguments. If the application account lacks backup privileges, use DBeaver's native backup with a privately configured authorized account. Verify the backup is nonempty and keep it private; it contains customer data. DBeaver can also export the existing table definitions/row counts for comparison. No automatic restore/reset operation is included.

## Review and apply

Review `database/20261026_AddGuestCheckouts.sql`. It creates one new table; it does not alter/drop/recreate any existing table. `EnsureCreatedAsync` does not upgrade an existing database. We intentionally do not introduce an EF initial migration against the preexisting EnsureCreated database.

```bash
cd ~/Downloads/ShopQevora
dotnet run --project src/ShopQevora.Api -- --audit-guest-schema
dotnet run --project src/ShopQevora.Api -- --apply-guest-schema
```

The explicit command verifies that Orders is InnoDB, its Id is signed int, and UserId has no foreign key that would reject guest ownership. It takes a MySQL advisory lock, validates an already-present GuestCheckouts table, and applies the reviewed CREATE TABLE only if absent. It stops on incompatible existing schemas. MySQL DDL is not transactionally rolled back: this is why the operation is additive and a backup procedure is required. The command never calls EnsureDeleted or Migrate and never changes User Secrets.

On a completely empty Development database, EnsureCreated may create the whole model as before. Existing installations need the explicit companion-table upgrade. If missing, guest checkout returns 503 with a safe operator message; authenticated shopping remains available. The application account needs CREATE and REFERENCES for the upgrade, or an authorized database operator must execute the SQL after the same schema checks.

## Privacy and retention

Guest contact information is necessary demo order data in MySQL. Restrict database and backup access and remove fictional personal data before public demonstrations. No order details are exposed via sequential IDs or anonymous lookup. The random checkout key is a private retry credential; do not share it. POST retries require the same key and normalized complete request and return the same existing order; changed requests with a reused key receive 409. Production requires HTTPS, a retention policy and operational safeguards; no payment processor is integrated.

## CustomerProfiles companion table

The professional account enhancement adds one separate `CustomerProfiles` table; Identity user columns/passwords and all commerce tables remain unchanged. The local upgrade was applied after a full private `mysqldump --single-transaction` backup. SQL: `database/20261026_AddCustomerProfiles.sql`.

```bash
cd ~/Downloads/ShopQevora
dotnet run --project tools/ShopQevora.E2E -- upgrade-profiles
```

The explicit local helper reads the existing User Secrets internally, verifies the Identity Id collation and uses that collation for the new foreign key. Backup output is mode 0600 in the owner's mode-0700 `~/.local/share/ShopQevora/backups` directory, outside source control. It refuses non-local hosts, failed backups and unexpected Identity key collation; it does not use EnsureCreated to change an existing schema. Repeated execution exits when the table already exists. Windows operators must provide a reviewed private backup/manual upgrade instead. Do not restore the backup over live customer data without a separate reviewed recovery procedure.

The table stores names, one default address, normalized phone/region, pending email and a sanitized avatar. Phone/email ownership is not marked verified. Profile endpoints resolve the owner exclusively from JWT claims; clients never choose a profile owner ID. Deleting a disposable E2E user cascades only its own profile. No account-deletion feature was added.

## ContactInquiries companion table

The Contact Us feature adds only `ContactInquiries`; it does not alter or delete existing Identity, product, cart, order, guest-checkout or profile records. Review `database/20261026_AddContactInquiries.sql` and run:

```bash
dotnet run --project tools/ShopQevora.E2E -- upgrade-contact
```

The command uses the existing User Secrets internally, takes a schema advisory lock, saves a full private MySQL backup outside the repository, creates the table, and verifies original table row counts. It refuses failed backups and unexpected existing schema shape. Repeated execution audits the table without modifying data. Windows/remote environments require a reviewed private backup/manual upgrade. This was applied locally with unchanged original row counts. See CONTACT.md for exact limits, storage/privacy behavior and operational constraints.
