import {test,expect,home,shot} from './helpers';
test('keyboard menu, real links and back/forward navigation',async({page})=>{
 await page.setViewportSize({width:375,height:667});await home(page);const menu=page.locator('[aria-controls="navigation"]');await expect(menu).toHaveAccessibleName(/menu/);await menu.click();await expect(menu).toHaveAttribute('aria-expanded','true');await shot(page,'mobile-navigation');await page.keyboard.press('Escape');await expect(menu).toHaveAttribute('aria-expanded','false');await expect(menu).toBeFocused();
 await menu.click();await page.locator('#navigation').getByRole('link',{name:'Sign In',exact:true}).click();await expect(page).toHaveURL(/\/login$/);await expect(page.getByRole('heading',{name:'Sign in',exact:true})).toBeVisible();
 await page.goBack();await expect(page.locator('#collection .product-card')).toHaveCount(36);await page.goForward();await expect(page).toHaveURL(/\/login$/);
 await page.goto('/login');await expect(page.getByRole('heading',{name:'Sign in',exact:true})).toBeVisible();await shot(page,'login');await page.goto('/register');await expect(page.getByRole('heading',{name:'Create an account'})).toBeVisible();await shot(page,'registration');
});
