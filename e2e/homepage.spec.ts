import {test,expect,home,shot} from './helpers';
test('premium identity, five real hero photographs and complete homepage',async({page})=>{
 await home(page);await expect(page).toHaveTitle(/ShopQevora/);await expect(page.locator('.hero-slide')).toHaveCount(5);await expect(page.locator('header .brand img')).toHaveAttribute('alt','ShopQevora');
 expect(await page.locator('.hero-background').evaluateAll(images=>images.every(i=>(i as HTMLImageElement).complete&&(i as HTMLImageElement).naturalWidth>0))).toBe(true);
 for(const id of ['collections','favorites','new-arrivals','story','collection'])await expect(page.locator('#'+id)).toBeAttached();
 await expect(page.locator('body')).not.toContainText(/ShopEase|Shop Ease/);await expect(page.locator('.image-corner').first()).toHaveText('SQ');
 const favicon=await page.request.get('/favicon.svg');expect(favicon.ok()).toBeTruthy();expect(await favicon.text()).toContain('ShopQevora');await shot(page,'homepage-top');
});
test('carousel autoplay, arrows, dots, hover/focus and explicit pause/resume',async({page},testInfo)=>{
 await home(page);await expect(page.locator('.hero-dot').first()).toBeEnabled();await page.mouse.move(0,0);
 const heroBox=await page.locator('.hero-carousel').boundingBox();const current=()=>page.locator('.hero-slide.is-active').getAttribute('aria-label');const first=await current();await expect.poll(current,{timeout:3500}).not.toBe(first);
 expect(await page.locator('.hero-carousel').boundingBox()).toEqual(heroBox);await page.locator('.hero-dot').nth(0).click();await expect.poll(current).toBe('1 of 5');await page.getByRole('button',{name:'Next slide',exact:true}).click();await expect.poll(current).toBe('2 of 5');await page.getByRole('button',{name:'Previous slide',exact:true}).click();await expect.poll(current).toBe('1 of 5');
 await page.locator('.hero-dot').nth(3).click();await expect.poll(current).toBe('4 of 5');await page.waitForTimeout(2200);expect(await current()).toBe('4 of 5');
 await page.getByRole('button',{name:'Pause automatic slides'}).click();await page.locator('#main').focus();await page.mouse.move(0,0);const paused=await current();await page.waitForTimeout(2300);expect(await current()).toBe(paused);
 await page.getByRole('button',{name:'Resume automatic slides'}).click();await page.locator('#main').focus();await page.mouse.move(0,0);await expect.poll(current,{timeout:3500}).not.toBe(paused);
 await page.locator('.hero-carousel').hover();const hovered=await current();await page.waitForTimeout(2200);expect(await current()).toBe(hovered);
 await page.getByRole('button',{name:'Next slide',exact:true}).focus();await page.mouse.move(0,0);const focused=await current();await page.waitForTimeout(2200);expect(await current()).toBe(focused);
 await page.emulateMedia({reducedMotion:'reduce'});await page.locator('#main').focus();const reduced=await current();await page.waitForTimeout(2200);expect(await current()).toBe(reduced);
 await page.locator('.hero-dot').nth(1).click();await page.getByRole('link',{name:'Explore Electronics',exact:true}).click();await expect(page.locator('#category')).toHaveValue('Electronics');await expect(page.locator('#collection .product-card')).toHaveCount(5);await shot(page,'hero-cta-category');
});
