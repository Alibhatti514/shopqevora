import AxeBuilder from '@axe-core/playwright';
import {test,expect,home,add,bag,guestForm,fillGuest,shot} from './helpers';
test('axe WCAG scan of homepage, authentication, cart and checkout',async({page})=>{
 await home(page);await page.emulateMedia({reducedMotion:'reduce'});await page.locator('#collection').scrollIntoViewIfNeeded();
 async function scan(label:string){const result=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa','wcag22aa']).analyze();await test.info().attach(`axe-${label}`,{body:JSON.stringify(result.violations,null,2),contentType:'application/json'});expect(result.violations,`axe violations on ${label}`).toEqual([]);}
 await scan('homepage');await add(page);await bag(page);await scan('cart');await guestForm(page);await fillGuest(page);await scan('checkout');await page.goto('/login');await scan('login');await page.goto('/register');await scan('registration');
});
test('skip link, named controls, keyboard focus and hero heading hierarchy',async({page})=>{
 await home(page);await page.locator('.skip-link').focus();await expect(page.locator('.skip-link')).toBeVisible();await page.keyboard.press('Enter');await expect(page.locator('#main')).toBeFocused();expect(await page.locator('#main').evaluate(e=>getComputedStyle(e).outlineColor)).toBe('rgb(36, 92, 79)');
 await page.locator('.hero-dot').nth(1).click();await expect(page.getByRole('heading',{level:1})).toHaveCount(1);await expect(page.getByRole('button',{name:'Next slide',exact:true})).toBeVisible();
 const names=await page.locator('button').evaluateAll(buttons=>buttons.filter(b=>!b.closest('[aria-hidden="true"]')&&!b.getAttribute('aria-label')&&!b.textContent?.trim()).length);expect(names).toBe(0);
});
