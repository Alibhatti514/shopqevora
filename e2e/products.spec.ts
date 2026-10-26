import {test,expect,home,API,shot} from './helpers';
import baseline from '../output/database-image-audit.json';
test('authoritative catalog is unchanged; search, combined category and sorting are real',async({page})=>{
 await home(page);const products=await(await page.request.get(API+'/api/products')).json();expect(products).toHaveLength(36);for(const original of baseline)expect(products.find((p:any)=>p.productID===original.productID)).toEqual(original);expect(new Set(products.map((p:any)=>p.name)).size).toBe(36);
 await page.locator('#search').fill('headphones');await expect(page.locator('#collection .product-card')).toHaveCount(2);await expect(page.locator('#collection .product-card h3')).toHaveText(['Studio Headphones','Arc Over-Ear Stand']);await expect(page.locator('#collection')).toContainText('$129.00');
 await page.locator('#category').selectOption('Home');await expect(page.getByRole('heading',{name:'Nothing here just yet.'})).toBeVisible();await page.getByRole('button',{name:'Reset your search'}).click();
 await page.locator('#search').fill('everyday');await page.locator('#category').selectOption('Fashion');await expect(page.locator('#collection .product-card')).toHaveCount(1);await expect(page.locator('#collection .product-card h3')).toHaveText('Everyday Cotton Cap');
 await page.getByRole('button',{name:/Clear filters/}).click();await page.locator('#sort').selectOption('low');const prices=await page.locator('#collection .product-title strong').allTextContents();expect(prices.map(p=>Number(p.replace(/[^\d.]/g,'')))).toEqual(products.map((p:any)=>p.price).sort((a:number,b:number)=>a-b));
 await page.locator('#search').fill('definitely-no-match');await expect(page.locator('.empty')).toContainText('Nothing here just yet');await page.getByRole('button',{name:'Reset your search'}).click();await expect(page.locator('#collection .product-card')).toHaveCount(36);
 await expect(page.locator('.section-note')).toContainText('rather than measured sales rankings');
 await page.locator('#collections').getByRole('link').filter({hasText:'Home & Living'}).click();await expect(page.locator('#category')).toHaveValue('Home');await shot(page,'product-collection');
});
test('all catalog pictures decode and product details agree with real API',async({page})=>{
 await home(page);for(const card of await page.locator('#collection .product-card').all()){await card.scrollIntoViewIfNeeded();const img=card.locator('img');await expect.poll(()=>img.evaluate((i:HTMLImageElement)=>i.complete&&i.naturalWidth>0)).toBe(true);await expect(img).not.toHaveAttribute('alt','');}
 const product=await(await page.request.get(API+'/api/products/1')).json();expect(product.name).toBe('Studio Headphones');expect(product.price).toBe(129);await page.locator('#collection .product-card').first().scrollIntoViewIfNeeded();await shot(page,'product-card-details');
});
