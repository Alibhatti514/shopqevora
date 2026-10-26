// Progressive enhancement: default markup remains visible if JS/observers are unavailable.
// Hero slides are deliberately outside the selector set and are never touched.
const instances = new WeakMap();
export function attach(root) {
    detach(root);
    if (!globalThis.IntersectionObserver || !globalThis.MutationObserver) return;
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    const nodes = new Set();
    let disposed = false, frame = 0, previousY = window.scrollY, direction = 'down';
    const show = element => { element.dataset.motionState = 'shown'; };
    const eligible = element => !element.closest('.hero-carousel') && !element.querySelector('[data-reveal]');
    const observer = new IntersectionObserver(entries => {
        if (disposed || media.matches) return;
        for (const entry of entries) {
            if (entry.isIntersecting && entry.intersectionRatio >= 0.12) {
                entry.target.dataset.direction = window.scrollY === previousY ? direction : window.scrollY < previousY ? 'up' : 'down';
                show(entry.target);
            } else if (entry.boundingClientRect.bottom <= 0 || entry.boundingClientRect.top >= window.innerHeight) {
                // Reset only once fully outside. Small scroll changes never erase visible text.
                entry.target.dataset.direction = window.scrollY === previousY ? direction : window.scrollY < previousY ? 'up' : 'down';
                entry.target.dataset.motionState = 'rest';
            }
        }
    }, { threshold: [0, 0.12], rootMargin: '0px' });
    const add = element => {
        if (nodes.has(element) || !eligible(element)) return;
        nodes.add(element);
        if (media.matches) { show(element); return; }
        const rect = element.getBoundingClientRect();
        element.dataset.direction = direction;
        element.dataset.motionState = rect.top >= window.innerHeight || rect.bottom <= 0 ? 'rest' : 'shown';
        element.classList.add('motion-enhanced');
        observer.observe(element);
    };
    const scan = () => {
        for (const element of nodes) if (!root.contains(element)) { observer.unobserve(element); nodes.delete(element); }
        root.querySelectorAll('[data-reveal], .section-head h2, .category-card, .product-card, .editorial-image, .editorial-copy h2, .value-card, .values-heading h2, .footer-grid h2').forEach((element, index) => {
            if (!element.dataset.reveal) element.dataset.reveal = element.matches('.product-card, .category-card, .value-card') ? 'card' : ['mask', 'fade', 'rise'][index % 3];
            add(element);
        });
    };
    const mutation = new MutationObserver(() => { if (!disposed) scan(); });
    const scroll = () => {
        if (frame || media.matches) return;
        frame = window.requestAnimationFrame(() => { frame = 0; direction = window.scrollY < previousY ? 'up' : 'down'; previousY = window.scrollY; });
    };
    const preference = () => {
        observer.disconnect();
        for (const element of nodes) {
            show(element);
            if (media.matches) element.classList.remove('motion-enhanced');
            else { element.classList.add('motion-enhanced'); observer.observe(element); }
        }
    };
    scan();
    mutation.observe(root, { childList: true, subtree: true });
    window.addEventListener('scroll', scroll, { passive: true });
    media.addEventListener('change', preference);
    instances.set(root, () => {
        disposed = true; observer.disconnect(); mutation.disconnect();
        if (frame) window.cancelAnimationFrame(frame);
        window.removeEventListener('scroll', scroll); media.removeEventListener('change', preference);
        for (const element of nodes) { element.classList.remove('motion-enhanced'); delete element.dataset.motionState; delete element.dataset.direction; }
        nodes.clear();
    });
}
export function detach(root) { instances.get(root)?.(); instances.delete(root); }
