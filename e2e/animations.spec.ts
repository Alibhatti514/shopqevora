import {test,expect,home,shot} from './helpers';
test('slow down/up reveals, repeated navigation and history leave no hidden content',async({page})=>{
 await home(page);await page.locator('.hero-control').last().click();await page.locator('#main').focus();
 for(let cycle=0;cycle<2;cycle++){
  for(const id of ['collections','favorites','new-arrivals','story','collection']){
   await page.locator('#'+id).scrollIntoViewIfNeeded();await page.waitForTimeout(850);
   const hidden=await page.locator('#'+id+' .motion-enhanced').evaluateAll(elements=>elements.filter(el=>{const r=el.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight&&r.height>0&&Number(getComputedStyle(el).opacity)<.9;}).map(el=>el.textContent?.slice(0,60)));
   expect(hidden,`Permanently hidden content in ${id}`).toEqual([]);
  }
  await page.locator('footer').scrollIntoViewIfNeeded();await page.waitForTimeout(850);await shot(page,'footer');await page.emulateMedia({reducedMotion:'no-preference'});
  await page.locator('#collections h2').scrollIntoViewIfNeeded();await expect(page.locator('#collections h2')).toHaveAttribute('data-motion-state','shown');await expect(page.locator('#collections h2')).toHaveAttribute('data-direction','up');
 }
 await page.goto('/login');await expect(page.getByRole('heading',{name:'Sign in',exact:true})).toBeVisible();await page.goBack();await expect(page.locator('#collection .product-card')).toHaveCount(36);await page.locator('#collection h2').scrollIntoViewIfNeeded();await page.waitForTimeout(1000);expect(await page.locator('#collection .motion-piece').evaluateAll(elements=>elements.every(e=>Number(getComputedStyle(e).opacity)===1))).toBe(true);
});
test('reduced motion keeps all text and controls available without autoplay',async({page})=>{
 await page.emulateMedia({reducedMotion:'reduce'});await home(page);const active=await page.locator('.hero-slide.is-active').getAttribute('aria-label');await page.waitForTimeout(2200);expect(await page.locator('.hero-slide.is-active').getAttribute('aria-label')).toBe(active);
 await page.locator('#collection').scrollIntoViewIfNeeded();expect(await page.locator('.motion-piece').evaluateAll(nodes=>nodes.every(n=>Number(getComputedStyle(n).opacity)===1))).toBe(true);await expect(page.locator('#collection .product-add').first()).toBeEnabled();
});

test('SPA history disposes hero timers and never duplicates motion observers',async({page})=>{
 await page.addInitScript(()=>{
  const diagnostics={timers:new Set<number>(),intersection:new Set<object>(),mutation:new Set<object>()};(window as any).__motionDiagnostics=diagnostics;
  const set=window.setInterval.bind(window),clear=window.clearInterval.bind(window);
  window.setInterval=((callback:any,delay?:number,...args:any[])=>{const id=set(callback,delay,...args);if(delay===2000&&String(callback).includes('Advance'))diagnostics.timers.add(Number(id));return id;}) as typeof window.setInterval;
  window.clearInterval=((id:any)=>{diagnostics.timers.delete(Number(id));clear(id);}) as typeof window.clearInterval;
  const IO=window.IntersectionObserver;
  window.IntersectionObserver=class extends IO{owned=false;constructor(callback:IntersectionObserverCallback,options?:IntersectionObserverInit){super(callback,options);this.owned=Array.isArray(options?.threshold)&&options!.threshold.includes(.12);if(this.owned)diagnostics.intersection.add(this);}observe(target:Element){if(this.owned)diagnostics.intersection.add(this);super.observe(target);}disconnect(){diagnostics.intersection.delete(this);super.disconnect();}};
  const MO=window.MutationObserver;
  window.MutationObserver=class extends MO{observe(target:Node,options?:MutationObserverInit){if(target instanceof Element&&target.classList.contains('motion-root'))diagnostics.mutation.add(this);super.observe(target,options);}disconnect(){diagnostics.mutation.delete(this);super.disconnect();}};
 });
 await home(page);await page.mouse.move(0,0);
 const counts=()=>page.evaluate(()=>{const d=(window as any).__motionDiagnostics;return{timers:d.timers.size,intersection:d.intersection.size,mutation:d.mutation.size};});
 await expect.poll(counts).toEqual({timers:1,intersection:1,mutation:1});
 for(let i=0;i<3;i++){
  await page.locator('footer a[href="/orders"]').click();await expect(page).toHaveURL(/\/orders$/);await expect.poll(counts).toEqual({timers:0,intersection:1,mutation:1});
  await page.goBack();await expect(page.locator('#collection .product-card')).toHaveCount(36);await page.mouse.move(0,0);await expect.poll(counts).toEqual({timers:1,intersection:1,mutation:1});
 }
});

test('slow repeated wheel scrolling in both directions keeps visible content readable',async({page})=>{
 test.setTimeout(150000);await home(page);await page.getByRole('button',{name:'Pause automatic slides'}).click();await page.locator('#main').focus();
 const height=await page.evaluate(()=>document.documentElement.scrollHeight-innerHeight);
 for(let cycle=0;cycle<2;cycle++){
  for(let y=0;y<height;y+=300){await page.mouse.wheel(0,300);await page.waitForTimeout(200);}
  await page.waitForTimeout(800);for(let y=height;y>0;y-=300){await page.mouse.wheel(0,-300);await page.waitForTimeout(200);}
 }
 await page.locator('#collections h2').scrollIntoViewIfNeeded();await expect(page.locator('#collections h2')).toHaveAttribute('data-motion-state','shown');await expect.poll(()=>page.locator('#collections h2').evaluate(e=>Number(getComputedStyle(e).opacity))).toBe(1);
});
