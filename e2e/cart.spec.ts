import {test,expect,home,add,bag,shot} from './helpers';
test('guest add, increase, decrease, remove and refresh persistence',async({page})=>{
 await home(page);await add(page);await add(page,'Ceramic Coffee Cup');await bag(page);await expect(page.locator('.cart-row')).toHaveCount(2);
 const headphones=page.locator('.cart-row').filter({hasText:'Studio Headphones'});await headphones.getByRole('button',{name:/Increase quantity/}).click();await expect(headphones.locator('.qty')).toHaveValue('2');await headphones.getByRole('button',{name:/Decrease quantity/}).click();await expect(headphones.locator('.qty')).toHaveValue('1');
 await page.locator('.cart-row').filter({hasText:'Ceramic Coffee Cup'}).getByRole('button',{name:'Remove'}).click();await expect(page.locator('.cart-row')).toHaveCount(1);await page.reload();await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('.qty')).toHaveValue('1');await expect(page.locator('.summary')).toContainText('$129.00');await shot(page,'guest-cart');
 await page.getByRole('button',{name:'Remove',exact:true}).click();await expect(page.getByRole('heading',{name:'Room for something good.'})).toBeVisible();
});
test('invalid quantities cannot change persisted quantities or totals',async({page})=>{
 await home(page);await add(page);await bag(page);await page.locator('.qty').fill('0');await page.locator('.qty').press('Tab');await expect(page.getByRole('alert')).toContainText('1 to 99');await expect(page.locator('.summary')).toContainText('$129.00');await page.reload();await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('.qty')).toHaveValue('1');
 await page.locator('.qty').fill('100');await page.locator('.qty').press('Tab');await expect(page.getByRole('alert')).toContainText('1 to 99');await page.reload();await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('.qty')).toHaveValue('1');
});
