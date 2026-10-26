# Coursera submission checklist

- [ ] Run `bash scripts/check.sh` successfully.
- [ ] Run API and client and capture working screenshots.
- [ ] Run `bash scripts/smoke.sh` successfully.
- [ ] Manually test register/login, cart refresh, checkout, orders and responsiveness.
- [ ] Review security and describe limitations honestly.
- [ ] For Parts 2–5, prepare a DOCX/PDF containing the actual relevant code and explanations, as requested by the course.
- [ ] In final reflection identify three features, challenges, architecture, security and state/performance.
- [ ] Record any real Microsoft Copilot usage, if applicable.
- [ ] Complete required peer reviews.

## Redesign verification

- [ ] Verify the authoritative course rubric (not present in this workspace).
- [ ] Confirm the course environment has .NET 8; all projects now target net8.0.
- [ ] Check all responsive sizes and browser flows in the README manual checklist.
- [ ] Inspect existing users/orders and the additive 20-item demo catalog in DBeaver.
- [ ] Include local image licensing/source documentation with the project.
- [ ] Exclude bin/obj, local secrets, tokens, and private screenshots from submission.

## ShopQevora guest checkout and rebrand

- [ ] Review DATABASE_UPGRADE.md and preserve a private verified backup.
- [ ] Verify GuestCheckouts companion table and existing Identity/orders/cart records in DBeaver.
- [ ] Test guest persistence, failure/retry, checkout confirmation and maximum-quantity sign-in merge.
- [ ] Review SVG identity, reverse scroll motion and reduced-motion support.
- [ ] Use ShopQevora.sln and the renamed startup paths.
- [ ] Distinguish automated Chrome evidence from remaining manual review.

## Part 9 browser evidence

- [x] Actual app tested with Playwright Test/TypeScript against native MySQL.
- [x] Chromium, Firefox, WebKit and representative touch projects executed.
- [x] All fourteen requested responsive sizes exercised.
- [x] Final 128 browser tests passed with no failures/skips; xUnit 55 and JavaScript 8 passed.
- [x] axe scans, keyboard checks, errors/network audits and screenshots collected.
- [x] Run-scoped fixtures cleaned; original catalog and customer/cart/order counts preserved.
- [ ] Complete manual physical-device/screen-reader review and authoritative rubric verification.
