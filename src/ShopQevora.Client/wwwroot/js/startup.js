(() => {
    'use strict';
    const app = document.getElementById('app');
    const screen = document.getElementById('startup-screen');
    const status = document.getElementById('startup-status');
    const error = document.getElementById('startup-error');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let runtimeReady = false, rendered = false, finished = false, leaving = false;
    let exitFallback, revealFallback;

    const observer = new MutationObserver(() => {
        if (!screen.isConnected && !finished) {
            // Reuse the removed placeholder for its short exit; never clone the app or mount twice.
            app.inert = true;
            app.style.opacity = '0';
            document.body.append(screen);
        }
        rendered = !!app.querySelector('#main');
        reveal();
    });
    observer.observe(app, { childList: true, subtree: true });

    function removeEntrance() {
        app.classList.remove('startup-content-enter');
        app.removeEventListener('animationend', removeEntrance);
        clearTimeout(revealFallback);
    }
    function finish() {
        if (finished) return;
        finished = true;
        clearTimeout(watchdog);
        clearTimeout(exitFallback);
        observer.disconnect();
        window.removeEventListener('error', resourceError, true);
        screen.removeEventListener('transitionend', onExit);
        const restoreFocus = screen.contains(document.activeElement);
        screen.remove();
        app.style.removeProperty('opacity');
        app.inert = false;
        if (!reducedMotion.matches) {
            app.classList.add('startup-content-enter');
            app.addEventListener('animationend', removeEntrance);
            revealFallback = setTimeout(removeEntrance, 250);
        }
        if (restoreFocus) app.querySelector('#main')?.focus({ preventScroll: true });
    }
    function onExit(event) { if (event.target === screen && event.propertyName === 'opacity') finish(); }
    function reveal() {
        if (!runtimeReady || !rendered || finished || leaving) return;
        leaving = true;
        clearTimeout(watchdog);
        screen.setAttribute('aria-hidden', 'true');
        screen.inert = true;
        if (reducedMotion.matches) { finish(); return; }
        screen.addEventListener('transitionend', onExit);
        screen.classList.add('startup-leaving');
        // Safety only: the transition normally removes the placeholder after 160ms.
        exitFallback = setTimeout(finish, 220);
    }
    function fail() {
        if (finished || leaving) return;
        clearTimeout(watchdog);
        screen.classList.add('startup-failed');
        screen.setAttribute('aria-busy', 'false');
        screen.removeAttribute('role');
        screen.removeAttribute('aria-live');
        status.hidden = true;
        error.hidden = false;
    }
    function resourceError(event) { if (event.target?.id === 'blazor-startup') fail(); }
    window.addEventListener('error', resourceError, true);
    const watchdog = setTimeout(fail, 45000);

    document.addEventListener('DOMContentLoaded', async () => {
        if (!window.Blazor?.start) { fail(); return; }
        try {
            await window.Blazor.start();
            runtimeReady = true;
            rendered = !!app.querySelector('#main');
            reveal();
        } catch {
            // Keep runtime/connection internals out of the customer-facing error message.
            fail();
        }
    }, { once: true });
})();
