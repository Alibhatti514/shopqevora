import AxeBuilder from '@axe-core/playwright';
import {test,expect,home,shot,noOverflow} from './helpers';
test('persistent cart and accessible menu with outside close, escape and focus return',async({page})=>{
 for(const width of [320,375,768,1440,2560]){
 await page.setViewportSize({width,height:900});await home(page);await expect(page.locator('.persistent-cart')).toBeVisible();await expect(page.locator('.persistent-cart')).toContainText('Cart');await expect(page.locator('.nav-actions .cart-count')).toHaveText('0');await noOverflow(page);
 const toggle=page.getByRole('button',{name:'Open menu'});await toggle.click();const dialog=page.getByRole('dialog',{name:'ShopQevora menu'});await expect(dialog).toBeVisible();await expect(dialog.getByRole('link',{name:'Sign In',exact:true})).toBeVisible();await expect(page.locator('header').getByRole('link',{name:'Sign In',exact:true})).toHaveCount(0);await expect(page.getByRole('button',{name:'Close menu'})).toBeFocused();await dialog.getByRole('link',{name:'Terms and Conditions'}).focus();await page.keyboard.press('Tab');await expect(page.getByRole('button',{name:'Close menu'})).toBeFocused();await shot(page,'drawer-'+width);await page.keyboard.press('Escape');await expect(dialog).toHaveCount(0);await expect(toggle).toBeFocused();expect(await page.evaluate(()=>document.body.style.overflow)).not.toBe('hidden');
 if(width>440){await toggle.click();await page.locator('.drawer-backdrop').click({position:{x:10,y:30}});await expect(dialog).toHaveCount(0);}
 }
});
test('price filters combine with category, search and all sorting directions',async({page})=>{
 await home(page);await page.getByRole('button',{name:/Filters \(/}).click();await page.getByLabel('Minimum price ($)',{exact:true}).fill('30');await page.getByLabel('Maximum price ($)',{exact:true}).fill('150');await page.getByRole('button',{name:/Show .* results/}).click();await page.locator('#category').selectOption('Electronics');await page.locator('#sort').selectOption('low');const cards=page.locator('#collection .product-card');expect(await cards.count()).toBeGreaterThan(1);
 const prices=async()=>cards.locator('.product-title strong').allTextContents();const values=async()=>(await prices()).map(s=>Number(s.replace(/[^0-9.]/g,'')));const low=await values();expect(low).toEqual([...low].sort((a,b)=>a-b));expect(low.every(v=>v>=30&&v<=150)).toBe(true);await page.locator('#sort').selectOption('high');await expect.poll(values).toEqual([...low].reverse());
 await page.locator('#sort').selectOption('name');const names=await cards.locator('h3').allTextContents();expect(names).toEqual([...names].sort((a,b)=>a.localeCompare(b)));await page.locator('#sort').selectOption('name-desc');await expect.poll(()=>cards.locator('h3').allTextContents()).toEqual([...names].reverse());
 await page.locator('#search').fill('no-product-matches');await expect(page.getByRole('heading',{name:'Nothing here just yet.'})).toBeVisible();await page.getByRole('button',{name:/Filters \(/}).click();await page.getByRole('button',{name:'Clear All',exact:true}).click();await page.getByRole('button',{name:/Show .* results/}).click();await expect(cards).toHaveCount(36);await expect(page.locator('#search')).toHaveValue('');
});

test('navigation and filter drawers meet automated accessibility checks',async({page})=>{
 await home(page);await page.emulateMedia({reducedMotion:'reduce'});await page.getByRole('button',{name:'Open menu'}).click();let scan=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa','wcag22aa']).analyze();expect(scan.violations).toEqual([]);await page.keyboard.press('Escape');
 await page.getByRole('button',{name:/Filters \(/}).click();scan=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa','wcag22aa']).analyze();expect(scan.violations).toEqual([]);await page.keyboard.press('Escape');await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('Shop All clears the current edit and every information link opens real demo content',async({page})=>{
 await home(page);await page.locator('#category').selectOption('Home');await page.getByRole('button',{name:'Open menu'}).click();await page.getByRole('dialog').getByRole('link',{name:'Shop All',exact:true}).click();await expect(page.locator('#category')).toHaveValue('');await expect(page.locator('#collection .product-card')).toHaveCount(36);
 for(const [slug,title] of [['about','About ShopQevora'],['contact','Contact Us'],['shipping','Shipping Information'],['returns','Returns and Refunds'],['privacy','Privacy Policy'],['terms','Terms and Conditions']]){
  await page.getByRole('button',{name:'Open menu'}).click();await page.getByRole('dialog').getByRole('link',{name:title,exact:true}).click();await expect(page).toHaveURL(new RegExp(slug==='contact'?'/contact$':'/information/'+slug+'$'));await expect(page.getByRole('heading',{name:title,exact:true})).toBeVisible();await expect(page.locator(slug==='contact'?'#contact-demo':'.information-copy')).toContainText(/demo/i);await expect(page.getByRole('dialog')).toHaveCount(0);await noOverflow(page);
 }
});
