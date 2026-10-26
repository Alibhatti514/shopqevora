# Original product assets

Tool: AI image-generation workflow; no fallback CLI or private API key was used.

Purpose: replace mismatched, branded, or inconsistent product photography with a consistent original visual set for fictional unbranded demo products. These are photoreal **synthetic visuals**, not evidence of real inventory. The hero uses real locally licensed landscape photography separately documented in HERO_SOURCES.md.

The exact prompt set is saved in `output/image-prompts.json`. Each item was generated with an individual call. Shared direction: square premium studio product still life, seamless warm ivory backdrop, soft daylight from upper left, a complete centered product with margins, realistic materials and subtle shadows, no logos/brands/marketing text/watermarks/collages. Subjects match each demo product.

Generated PNG source files: `output/product-originals/{headphones,lamp,backpack,watch,cup,sneaker,speaker,vase,candle,bottle,travel,tote,cap,sunglasses,hoodie,organizer,notebook,keyboard,charging}.png`.

Final web files: `src/ShopQevora.Client/wwwroot/images/products/{name}.webp` and `{name}-400.webp`. The `sneaker.png` source is published as `shoes.webp`. All other source names match final names. Full-size WebP images are 800×800; smaller images are 400×400. Proportions are preserved; the consistent card crop is handled by CSS. PNG source artifacts are not downloaded by storefront visitors.

Visual QA sheets are saved to `output/product-audit.jpg` and `output/hero-audit.jpg`. They are asset contact sheets, not fabricated screenshots of a running website.

## ShopQevora rebrand

All original product PNG/WebP and hero WebP assets were retained unchanged. Only the SVG brand identity was replaced; see BRAND_AND_MOTION.md for the SQ vector geometry, outlined lettering and restrained layered-depth treatment. No 3D model was created.

## Catalog expansion — 26 October 2026

Sixteen original product visualizations were generated with an AI image-generation tool for this project. These are illustrative, unbranded demo assets, not photographs of manufactured stock or claims about real products. No third-party photographer or license is invented. Original outputs remain in the original image-generation workspace; `output/catalog-expansion.json` records the exact original source per product. Each image was resized without stretching onto the existing 4:5 presentation, then encoded as local 800×1000 and 400×500 WebP files. Existing images were not edited.

Added stems under `src/ShopQevora.Client/wwwroot/images/products/`: headphone-stand, cushion, tray, coffee-set, carafe, duffel, passport, pillow, crossbody, belt, beanie, shirt, yoga-block, stretch-strap, laptop-sleeve, earbuds. Each stem has `.webp` and `-400.webp` derivatives. The headphone stand image shows headphones on the stand as presentation context; the demo listing is for the stand.

The expansion manifest also includes the exact final prompt for each asset, use case (`product-mockup`), image-generation method, original generated source and both final workspace asset paths. No CLI image-generation fallback was used. All final website-consumed images are in the project, not dependent on the original image-generation workspace.
