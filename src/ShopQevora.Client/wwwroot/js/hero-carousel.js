// One lifecycle per carousel; no global event handlers survive detach().
const instances = new WeakMap();
export function attach(root, dotnet, delay = 2000) {
    detach(root);
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const state = { paused: false, hover: root.matches(':hover'), focus: root.contains(document.activeElement), disposed: false, timer: null, pending: false, ready: false };
    const stop = () => { if (state.timer !== null) window.clearInterval(state.timer); state.timer = null; };
    const sync = () => {
        stop();
        if (!state.ready || state.disposed || state.paused || state.hover || state.focus || motion.matches || document.hidden) return;
        state.timer = window.setInterval(async () => {
            if (state.pending || state.disposed) return;
            state.pending = true;
            try { await dotnet.invokeMethodAsync('Advance'); }
            catch { stop(); }
            finally { state.pending = false; }
        }, delay);
    };
    const enter = () => { state.hover = true; sync(); };
    const leave = () => { state.hover = false; sync(); };
    const focus = () => { state.focus = true; sync(); };
    const blur = event => { state.focus = root.contains(event.relatedTarget); sync(); };
    const preference = () => { dotnet.invokeMethodAsync('MotionChanged', motion.matches).catch(stop); sync(); };
    root.addEventListener('mouseenter', enter);
    root.addEventListener('mouseleave', leave);
    root.addEventListener('focusin', focus);
    root.addEventListener('focusout', blur);
    motion.addEventListener('change', preference);
    document.addEventListener('visibilitychange', sync);
    instances.set(root, {
        pause(value) { state.paused = value; sync(); },
        dispose() {
            state.disposed = true; stop();
            root.removeEventListener('mouseenter', enter);
            root.removeEventListener('mouseleave', leave);
            root.removeEventListener('focusin', focus);
            root.removeEventListener('focusout', blur);
            motion.removeEventListener('change', preference);
            document.removeEventListener('visibilitychange', sync);
        }
    });
    // Do not advance into a photograph that has not decoded yet.
    Promise.all([...root.querySelectorAll('.hero-background')].map(image => image.decode()))
        .then(() => { if (!state.disposed) { state.ready = true; dotnet.invokeMethodAsync('ImagesReady').catch(stop); sync(); } })
        .catch(stop);
    preference();
}
export function setPaused(root, value) { instances.get(root)?.pause(value); }
export function detach(root) { instances.get(root)?.dispose(); instances.delete(root); }
