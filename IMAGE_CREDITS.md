# Image and type credits

## Product visuals

All current product visuals are original, unbranded AI-generated studio still lifes created with the built-in image-generation tool for this fictional demo catalog. They are **not photographs of stocked inventory** and do not represent a third-party manufacturer's products. No permission from a stock-photo licensor is asserted for these generated assets. See [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md) and `output/image-prompts.json` for provenance and the actual prompt set.

There are 19 unique product visuals for 20 products; Canvas Sneakers and Weekend Sneakers intentionally share the same relevant canvas-sneaker image. All are served locally as WebP at 800px and 400px widths. Original PNGs are retained under `output/product-originals` outside the web root.

The previous stock images, branded sneakers/speaker images, mismatched product photos, and blue starter SVG product illustrations were removed from the deployed asset set.

## Hero photographs

Five different landscape photographs were downloaded locally from Unsplash. Sources and license: [HERO_SOURCES.md](HERO_SOURCES.md), [Unsplash license](https://unsplash.com/license). They are used as editorial lifestyle scenes. No photographer names or permissions beyond that license are invented. The storefront does not hotlink them.

## Typography and identity

Manrope and Fraunces are stored locally under `src/ShopQevora.Client/wwwroot/fonts`. Sources are the Google Fonts repository:

- https://github.com/google/fonts/tree/main/ofl/manrope
- https://github.com/google/fonts/tree/main/ofl/fraunces

Their SIL Open Font License files are included beside the WOFF2 files. The ShopQevora SVG wordmark customizes outlined Fraunces glyphs and the intertwined SQ symbol is original vector geometry. Its restrained depth uses layered vector paths, not a 3D model. The logo has no dependency on external fonts or emoji.
