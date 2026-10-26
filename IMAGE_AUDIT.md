# Product image audit

All 20 original MySQL demo products have relevant original local WebP visuals. Original IDs/names/prices/categories/descriptions were compared before/after; only known starter image URLs were repaired. Every full/small image has been decoded successfully; no old blue illustrations or third-party branded product photos are used.

| ID | Product | Category | Local image |
|---|---|---|---|
| 1 | Studio Headphones | Electronics | `/images/products/headphones.webp` |
| 2 | Minimal Desk Lamp | Home | `/images/products/lamp.webp` |
| 3 | Everyday Backpack | Lifestyle | `/images/products/backpack.webp` |
| 4 | Smart Watch | Electronics | `/images/products/watch.webp` |
| 5 | Ceramic Coffee Cup | Home | `/images/products/cup.webp` |
| 6 | Canvas Sneakers | Lifestyle | `/images/products/shoes.webp` |
| 7 | Pocket Sound Speaker | Electronics | `/images/products/speaker.webp` |
| 8 | Sculpture Ceramic Vase | Home | `/images/products/vase.webp` |
| 9 | Evening Ritual Candle | Home | `/images/products/candle.webp` |
| 10 | Daily Hydration Bottle | Lifestyle | `/images/products/bottle.webp` |
| 11 | Weekender Travel Organizer | Accessories | `/images/products/travel.webp` |
| 12 | Market Canvas Tote | Lifestyle | `/images/products/tote.webp` |
| 13 | Weekend Sneakers | Fashion | `/images/products/shoes.webp` |
| 14 | Everyday Cotton Cap | Fashion | `/images/products/cap.webp` |
| 15 | Sunday Sunglasses | Accessories | `/images/products/sunglasses.webp` |
| 16 | Slow Morning Hoodie | Fashion | `/images/products/hoodie.webp` |
| 17 | Studio Desk Organizer | Workspace | `/images/products/organizer.webp` |
| 18 | Daily Notes Journal | Workspace | `/images/products/notebook.webp` |
| 19 | Quiet Wireless Keyboard | Workspace | `/images/products/keyboard.webp` |
| 20 | Desktop Charging Station | Electronics | `/images/products/charging.webp` |

19 unique original images cover 20 products. The two canvas-sneaker variants share one relevant image. All images also have a `-400.webp` counterpart. New unknown products without an audited image are omitted from the public grid, without deleting their database rows or order history; saved-cart unknowns use a neutral local unavailable-image visual.

Real editorial hero photography has five distinct landscape assets, plus mobile versions, under `wwwroot/images/hero`. See HERO_SOURCES.md for the license. See ASSET_PROVENANCE.md for the original synthetic product visuals and exact prompts.

## ShopQevora preservation check

The rebrand did not regenerate or modify any photograph. All 48 hero/product WebP files match their pre-rebrand SHA256 values; see `output/preserved-photography-sha256.json`. Product IDs, names, prices, categories and image paths remain unchanged. New SVG identity assets are separate from this catalog imagery.

## Catalog expansion audit — 26 October 2026

The public manifest now covers 36 products. Sixteen new unique image pairs were added; every new product maps to its own relevant local visualization. Existing 20 names, IDs, categories, prices, descriptions and image mappings were verified against `output/database-image-audit.json` before updating the E2E catalog fingerprint. Two seed passes produced exactly 36 records. New assets are 800×1000/400×500 WebPs with descriptive alt text in `ProductPresentation.CatalogVisuals`. A contact sheet was inspected at `test-output/catalog-contact-sheet.jpg`; this generated QA artifact is ignored. Browser decode assertions and .NET manifest checks cover both old and new assets.

| Added product | Category | Local image stem |
|---|---|---|
| Arc Over-Ear Stand | Workspace | `headphone-stand` |
| Linen Cushion Cover | Home | `cushion` |
| Pebble Catchall Tray | Home | `tray` |
| Pour Over Coffee Set | Home | `coffee-set` |
| Ribbed Glass Carafe | Home | `carafe` |
| Foldaway Travel Duffel | Travel | `duffel` |
| Passport Folio | Travel | `passport` |
| Cloud Travel Pillow | Travel | `pillow` |
| Everyday Crossbody | Accessories | `crossbody` |
| Braided Cotton Belt | Accessories | `belt` |
| Ribbed Knit Beanie | Fashion | `beanie` |
| Essential Cotton Shirt | Fashion | `shirt` |
| Cork Yoga Block | Wellness | `yoga-block` |
| Woven Stretch Strap | Wellness | `stretch-strap` |
| Slim Laptop Sleeve | Workspace | `laptop-sleeve` |
| Pocket Audio Earbuds | Electronics | `earbuds` |
