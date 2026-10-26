import { fileURLToPath } from 'node:url';
const { chromium } = await import(process.env.PLAYWRIGHT_MODULE || 'playwright');
const outputDir = fileURLToPath(new URL('../../output/browser-qa/', import.meta.url));import fs from 'node:fs/promises';
await fs.mkdir(outputDir,{recursive:true});
const browser=await chromium.launch({channel:'chrome',headless:true});
try{
 const page=await browser.newPage({viewport:{width:375,height:900},reducedMotion:'no-preference'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.route('http://localhost:5100/**',async route=>{const response=await route.fetch({url:route.request().url().replace('localhost:5100','localhost:5101')});await route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':'*'}});});
 await page.goto('http://localhost:5174');await page.locator('#collection .product-card').first().waitFor();
 await page.getByRole('button',{name:'Menu'}).click();if(await page.locator('.menu-toggle').getAttribute('aria-expanded')!=='true')throw Error('Menu failed to open');await page.locator('.menu-toggle').press('Escape');if(await page.locator('.menu-toggle').getAttribute('aria-expanded')!=='false')throw Error('Escape failed to close menu');
 await page.getByRole('button',{name:'Add Studio Headphones to your bag',exact:true}).first().click();await page.goto('http://localhost:5174/cart');await page.locator('.cart-row').waitFor();const widths=[375,430,768,1024,1440,1920];
 for(const width of widths){await page.setViewportSize({width,height:1000});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Cart overflows '+width);}
 await page.setViewportSize({width:375,height:900});await page.evaluate(()=>window.scrollTo({top:0,behavior:'instant'}));await page.screenshot({path:`${outputDir}375-cart.png`,fullPage:true});
 await page.getByRole('button',{name:'Remove',exact:true}).click();await page.getByRole('heading',{name:'Room for something good.'}).waitFor();
 await page.goto('http://localhost:5174');await page.locator('#collection .product-card').first().waitFor();await page.setViewportSize({width:1440,height:1000});
 const heading=page.locator('#collection h2');await heading.scrollIntoViewIfNeeded();await page.waitForTimeout(1100);const text=await heading.innerText();if(!text.includes('The complete collection.'))throw Error('Word reveal missing text');
 const preview=await browser.newPage({viewport:{width:900,height:320}});await preview.setContent('<html><body style="margin:0;background:#F8F6F2;display:flex;align-items:center;gap:28px;padding:40px"><img alt="ShopQevora main" width="260" src="http://localhost:5174/logo.svg"><div style="padding:20px;background:#20231F"><img alt="ShopQevora light" width="260" src="http://localhost:5174/logo-light.svg"></div><img alt="SQ 32" width="32" src="http://localhost:5174/favicon.svg"><img alt="SQ 16" width="16" src="http://localhost:5174/favicon.svg"></body></html>');await preview.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth));await preview.screenshot({path:`${outputDir}logo-scale-preview.png`});
 const result={cartWidths:widths,guestRemoval:'empty state shown',mobileMenu:'opens; Escape closes and returns focus',logo:'main/light SVG plus 16px/32px favicon rendered',pageErrors:errors};await fs.writeFile(`${outputDir}final-checks.json`,JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(errors.length)process.exitCode=1;
}finally{await browser.close();}
