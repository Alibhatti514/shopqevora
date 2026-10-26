import { fileURLToPath } from 'node:url';
const { chromium } = await import(process.env.PLAYWRIGHT_MODULE || 'playwright');
const outputDir = fileURLToPath(new URL('../../output/browser-qa/', import.meta.url));
import fs from 'node:fs/promises';
await fs.mkdir(outputDir,{recursive:true});
if(process.env.SHOPQEVORA_ALLOW_DEMO_ORDER !== 'yes') throw Error('This test creates a fictional MySQL guest order. Read tests/browser/README.md and opt in explicitly.');
const browser=await chromium.launch({channel:'chrome',headless:true});
const context=await browser.newContext({viewport:{width:1440,height:1000},reducedMotion:"no-preference"});
const errors=[],expectedConsole=[];let faultInjected=false;let lastGuest=null,lastReceipt=null,failNextGuest=false;
await context.route('http://localhost:5100/**',async route=>{
 const req=route.request();
 if(failNextGuest && req.url().endsWith('/api/guest-orders') && req.method()==='POST'){failNextGuest=false;faultInjected=true;await route.fulfill({status:503,contentType:'application/json',headers:{'access-control-allow-origin':'*'},body:JSON.stringify({message:'Temporary QA failure. Your bag is preserved.'})});return;}
 if(req.url().endsWith('/api/guest-orders')&&req.method()==='POST')lastGuest=req.postDataJSON();
 const response=await route.fetch({url:req.url().replace('localhost:5100','localhost:5101')});
 if(req.url().endsWith('/api/guest-orders')&&response.ok())lastReceipt=await response.json();
 await route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':'*','access-control-allow-headers':'*','access-control-allow-methods':'*'}});
});
const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'){if(faultInjected&&message.text().includes('503'))expectedConsole.push(message.text());else errors.push(message.text());}});
await page.goto('http://localhost:5174');await page.locator('#collection .product-card').first().waitFor();
await page.waitForFunction(()=>!document.querySelector('.hero-dot').disabled);
await page.mouse.move(0,0);
const start=await page.locator('.hero-slide.is-active').getAttribute('aria-label');await page.waitForTimeout(2200);const next=await page.locator('.hero-slide.is-active').getAttribute('aria-label');if(start===next)throw Error('Autoplay did not advance');
const sizes=[375,430,768,1024,1440,1920];const checks=[];
for(const width of sizes){
 await page.setViewportSize({width,height:width<600?900:1000});await page.evaluate(()=>window.scrollTo(0,0));await page.waitForTimeout(400);
 const metrics=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,logo:!!document.querySelector('.brand img').naturalWidth,hero:document.querySelector('.hero-carousel').getBoundingClientRect().width,cards:document.querySelectorAll('#collection .product-card').length}));
 if(metrics.scroll>width||!metrics.logo||Math.abs(metrics.hero-width)>1||metrics.cards!==20)throw Error('Responsive check failed '+JSON.stringify(metrics));
 await page.screenshot({path:`${outputDir}${width}-home.png`});checks.push(metrics);
}
await page.setViewportSize({width:1440,height:1000});await page.evaluate(()=>window.scrollTo(0,document.body.scrollHeight));await page.waitForTimeout(500);await page.locator('#collections h2').scrollIntoViewIfNeeded();await page.waitForTimeout(900);
const reverse=await page.locator('#collections h2').evaluate(el=>({state:el.dataset.motionState,direction:el.dataset.direction}));if(reverse.state!=='shown'||reverse.direction!=='up')throw Error('Reverse reveal failed '+JSON.stringify(reverse));
await page.emulateMedia({reducedMotion:'reduce'});await page.evaluate(()=>window.scrollTo(0,0));await page.waitForTimeout(300);const paused=await page.locator('.hero-slide.is-active').getAttribute('aria-label');await page.waitForTimeout(2200);if(paused!==await page.locator('.hero-slide.is-active').getAttribute('aria-label'))throw Error('Reduced-motion autoplay was not disabled');
// Real MySQL guest checkout with explicitly fictional QA details; never delete prior records.
await page.getByRole('button',{name:'Add Studio Headphones to your bag',exact:true}).first().click();
await page.reload();await page.locator('#collection .product-card').first().waitFor();await page.goto('http://localhost:5174/cart');await page.locator('.cart-row').waitFor();await page.locator('.qty').fill('2');await page.locator('.qty').press('Tab');await page.waitForTimeout(150);
await page.getByRole('button',{name:'Continue to checkout'}).click();await page.getByRole('button',{name:'Continue as Guest',exact:true}).click();
await page.setViewportSize({width:375,height:900});
for(const [label,value] of [['Full name','Portfolio QA Guest'],['Email address','guest-qa@example.test'],['Phone number','03000000000'],['Street address','Demo Address 1, Test Area'],['City','Karachi'],['Province / region','Sindh']])await page.getByLabel(label,{exact:true}).fill(value);
const checkoutSizes=[];
for(const width of sizes){await page.setViewportSize({width,height:width<600?900:1000});await page.evaluate(()=>{document.activeElement?.blur();window.scrollTo({top:0,behavior:'instant'});});await page.waitForTimeout(200);if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Checkout overflows '+width);checkoutSizes.push(width);await page.screenshot({path:`${outputDir}${width}-checkout.png`,fullPage:true});}
failNextGuest=true;await page.getByRole('button',{name:'Place guest demo order'}).click();await page.getByRole('alert').filter({hasText:'Your bag is preserved'}).waitFor();
const afterFailure=await page.evaluate(()=>JSON.parse(localStorage.getItem('shopqevora_guest_cart_v1')));if(afterFailure.Lines[0].Quantity!==2)throw Error('Failed request cleared bag');

await page.getByRole('button',{name:'Place guest demo order'}).click();await page.waitForURL('**/guest-confirmation');await page.locator('.order-reference').waitFor();
if(!lastReceipt||lastReceipt.total!==258||!lastReceipt.reference.startsWith('GQ-'))throw Error('Unexpected guest receipt');
const stored=await page.evaluate(()=>JSON.stringify({...localStorage,...sessionStorage}));if(stored.includes('guest-qa@example.test')||stored.includes('Portfolio QA Guest'))throw Error('Personal data stored in browser');
const cart=await page.evaluate(()=>JSON.parse(localStorage.getItem('shopqevora_guest_cart_v1')));if(cart.Lines.length!==0)throw Error('Guest cart did not clear');
const confirmationSizes=[];
for(const width of sizes){await page.setViewportSize({width,height:width<600?900:1000});await page.evaluate(()=>window.scrollTo({top:0,behavior:'instant'}));await page.waitForTimeout(200);if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Confirmation overflows '+width);confirmationSizes.push(width);await page.screenshot({path:`${outputDir}${width}-confirmation.png`,fullPage:true});}

const retry=await context.request.post('http://localhost:5101/api/guest-orders',{data:lastGuest});if(!retry.ok()||(await retry.json()).reference!==lastReceipt.reference)throw Error('Retry idempotency failed');
const report={responsive:checks,checkoutSizes,confirmationSizes,failedRequest:'bag preserved',reverse,autoplay:'advanced after 2200ms',reducedMotion:'autoplay stopped',guestOrderReference:lastReceipt.reference,guestTotal:lastReceipt.total,guestCart:'persisted, quantity updated, cleared after success',retry:'same reference',expectedInjectedFailureMessages:expectedConsole.length,consoleErrors:errors};
await fs.writeFile(`${outputDir}results.json`,JSON.stringify(report,null,2));
console.log(JSON.stringify(report));await browser.close();if(errors.length)process.exitCode=1;
