import {test,expect} from './helpers';

for(const [width,height] of [[320,568],[1440,900],[2560,1440]]){
 test(`premium startup at ${width}x${height}, held network then real homepage`,async({page})=>{
  await page.setViewportSize({width,height});let release!:()=>void;const gate=new Promise<void>(resolve=>release=resolve);
  await page.route('**/_framework/blazor.webassembly.js',async route=>{await gate;await route.continue();});
  await page.goto('/',{waitUntil:'commit'});
  try{
   const loader=page.getByRole('status',{name:'Loading ShopQevora',exact:true});await expect(loader).toBeVisible();await expect(loader.getByRole('heading',{name:'ShopQevora',exact:true})).toBeVisible();await expect(loader).toContainText('Discover Better. Shop Smarter.');
   await expect.poll(()=>loader.locator('img').evaluate((i:HTMLImageElement)=>i.complete&&i.naturalWidth>0)).toBe(true);await page.evaluate(()=>Promise.all([document.fonts.load('500 40px Fraunces'),document.fonts.load('400 10px Manrope')]));expect(await page.evaluate(()=>document.fonts.check('500 40px Fraunces')&&document.fonts.check('400 10px Manrope'))).toBe(true);await page.waitForTimeout(650);
   expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBe(width);await expect(loader.locator('.startup-indicator')).not.toHaveAttribute('aria-valuenow',/./);
   expect(await loader.locator('.startup-letter').evaluateAll(chars=>chars.every(c=>Number(getComputedStyle(c).opacity)===1))).toBe(true);
   const path=test.info().outputPath(`loading-${width}.png`);await page.screenshot({path});await test.info().attach(`premium-loading-${width}`,{path,contentType:'image/png'});
  }finally{release();}
  await expect(page.locator('#collection .product-card')).toHaveCount(36);await expect(page.locator('#startup-screen')).toHaveCount(0,{timeout:2000});expect(await page.locator('#app').evaluate((e:HTMLElement)=>e.inert)).toBe(false);await expect(page.locator('#app')).toHaveCSS('opacity','1');
 });
}
test('normal startup removes the loader without a retained overlay',async({page})=>{
 await page.goto('/');await expect(page.locator('#collection .product-card')).toHaveCount(36);await expect(page.locator('#startup-screen')).toHaveCount(0,{timeout:2000});await expect(page.locator('.startup-content-enter')).toHaveCount(0);await expect(page.locator('#collection .product-add').first()).toBeEnabled();
});
test('reduced-motion startup is static and hands off immediately',async({page})=>{
 await page.emulateMedia({reducedMotion:'reduce'});let release!:()=>void;const gate=new Promise<void>(r=>release=r);await page.route('**/_framework/blazor.webassembly.js',async route=>{await gate;await route.continue();});await page.goto('/',{waitUntil:'commit'});
 try{await expect(page.locator('#startup-screen')).toBeVisible();expect(await page.locator('.startup-mark img').evaluate(e=>getComputedStyle(e).animationName)).toBe('none');expect(await page.locator('.startup-indicator').evaluate(e=>getComputedStyle(e,'::after').animationName)).toBe('none');expect(await page.locator('.startup-letter').evaluateAll(chars=>chars.every(c=>Number(getComputedStyle(c).opacity)===1))).toBe(true);}finally{release();}
 await expect(page.locator('#collection .product-card')).toHaveCount(36);await expect(page.locator('#startup-screen')).toHaveCount(0);
});
test('failed framework download offers accessible reload and recovers',async({page,audit})=>{
 audit.allow('/_framework/blazor.webassembly.js',503);await page.route('**/_framework/blazor.webassembly.js',route=>route.fulfill({status:503,body:''}));await page.goto('/');await expect(page.getByRole('alert').filter({hasText:'We couldn’t open the store'})).toBeVisible();await expect(page.getByRole('button',{name:'Reload the store'})).toBeVisible();await expect(page.locator('#startup-screen')).toHaveAttribute('aria-busy','false');
 const path=test.info().outputPath('startup-error.png');await page.screenshot({path});await test.info().attach('startup-retry',{path,contentType:'image/png'});await page.unroute('**/_framework/blazor.webassembly.js');await page.getByRole('button',{name:'Reload the store'}).click();await expect(page.locator('#collection .product-card')).toHaveCount(36);await expect(page.locator('#startup-screen')).toHaveCount(0);
});
test('failed startup helper also provides a working retry',async({page,audit})=>{
 audit.allow('/js/startup.js',503);await page.route('**/js/startup.js',route=>route.fulfill({status:503,body:''}));await page.goto('/');await expect(page.getByRole('button',{name:'Reload the store'})).toBeVisible();await page.unroute('**/js/startup.js');await page.getByRole('button',{name:'Reload the store'}).click();await expect(page.locator('#collection .product-card')).toHaveCount(36);await expect(page.locator('#startup-screen')).toHaveCount(0);
});
