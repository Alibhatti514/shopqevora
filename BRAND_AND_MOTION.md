# ShopQevora identity and motion

The existing theme, palette, site fonts, layouts, product cards/grids, photography and five-slide/two-second hero are preserved. New styles are limited to logo integration, scroll enhancement, and guest checkout.

## Logo

`src/ShopQevora.Client/wwwroot/logo.svg` and `logo-light.svg` contain a layered, intertwined SQ ribbon and an outlined wordmark with deliberate pair spacing. Knockout crossings alternate which stroke appears in front; a narrow highlight and a 1–2px offset silhouette imply restrained depth. The Q has an unmistakable diagonal tail. This is vector layering, not a genuine 3D model. The lettering is customized from local Fraunces outlines (OFL license retained); it does not depend on browser fonts. The symbol is original geometry.

`monogram.svg` / `monogram-light.svg` provide standalone symbols. `favicon.svg` simplifies depth/highlights and uses a warm-ivory rounded field for small-size contrast. All are pure SVG paths with local accessible titles; no remote/raster/font dependencies. Main and dark-background colors come from the existing theme.

## Scroll module

`Components/RevealText.razor` generates stable word/character spans in Blazor, instead of mutating framework-owned text nodes in JavaScript. Full text is retained in a screen-reader-only span; visual pieces are aria-hidden. Stagger delays are capped at 700ms. Only selected short captions are typed; prices, forms and essential instructions are not delayed.

`wwwroot/js/scroll-motion.js` uses one IntersectionObserver and one MutationObserver per mounted layout. Default HTML is visible. Eligible offscreen nodes opt into CSS enhancement. Elements still partly within the viewport are not reset; fully exited elements can replay when approached from either direction. Scroll direction is sampled with one passive listener and requestAnimationFrame. Pending frames/listeners/observers are removed on disposal/remount. Removed Blazor nodes are unobserved. Hero slides are explicitly excluded.

CSS reveals use opacity, independent translate and restrained clipping, preserving the existing card hover transform. The mask retains enough geometry for viewport detection, avoiding permanently invisible clipped targets. Keyboard focus reveals content immediately; reduced-motion changes disconnect reveal observation and show all text/cards. No layout dimensions are animated and no heavy animation library was added.

MainLayout initializes the module once after rendering and disposes it. Router focus targets the main content container to preserve accessible navigation without falsely triggering the carousel's focus pause on initial arrival.
