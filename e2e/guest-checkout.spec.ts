import {test,expect,home,add,bag,guestForm,fillGuest,guestRequest,API,shot} from './helpers';
test('complete real guest checkout, summary, duplicate retry and privacy',async({page})=>{
 await home(page);await add(page);await add(page,'Ceramic Coffee Cup');await bag(page);await page.locator('.cart-row').filter({hasText:'Studio Headphones'}).getByRole('button',{name:/Increase quantity/}).click();await page.locator('.cart-row').filter({hasText:'Ceramic Coffee Cup'}).getByRole('button',{name:'Remove'}).click();await page.reload();await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('.qty')).toHaveValue('2');await guestForm(page);const addressEmail=await fillGuest(page);await expect(page.locator('.summary')).toContainText('$258.00');await shot(page,'guest-checkout');
 let sent:any;page.on('request',request=>{if(request.url().endsWith('/api/guest-orders')&&request.method()==='POST')sent=request.postDataJSON();});
 await page.getByRole('button',{name:'Place guest demo order'}).click();await expect(page).toHaveURL(/guest-confirmation$/);await expect(page.locator('.order-reference')).toHaveText(/^GQ-[A-F0-9]{32}$/);await expect(page.locator('main')).toContainText('$258.00');await expect(page.locator('main')).toContainText('No payment was taken');await expect(page.locator('address')).toContainText('Karachi');await shot(page,'guest-confirmation');
 const reference=await page.locator('.order-reference').innerText();expect(sent).toBeTruthy();const retry=await page.request.post(API+'/api/guest-orders',{data:sent});expect(retry.status()).toBe(200);expect((await retry.json()).reference).toBe(reference);
 expect((await page.request.get(API+'/api/guest-orders/'+reference)).status()).toBe(404);expect((await page.request.get(API+'/api/orders')).status()).toBe(401);
 const storage=await page.evaluate(()=>JSON.stringify({...localStorage,...sessionStorage}));expect(storage).not.toContain(addressEmail);expect(await page.evaluate(()=>JSON.parse(localStorage.getItem('shopqevora_guest_cart_v1')!).Lines)).toEqual([]);
 await page.reload();await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.getByRole('heading',{name:'Your order details stay private.'})).toBeVisible();
});
test('empty checkout and required/email validation preserve guest bag',async({page})=>{
 await page.goto('/checkout');await expect(page.getByRole('heading',{name:'Your bag is waiting.'})).toBeVisible();await home(page);await add(page);await bag(page);await guestForm(page);
 await page.getByRole('button',{name:'Place guest demo order'}).click();await expect(page.locator('form .error')).toContainText('required');await fillGuest(page);await page.locator('#guest-email').fill('invalid-email');await page.getByRole('button',{name:'Place guest demo order'}).click();expect(await page.locator('#guest-email').evaluate((i:HTMLInputElement)=>i.validity.valid)).toBe(false);expect(await page.evaluate(()=>JSON.parse(localStorage.getItem('shopqevora_guest_cart_v1')!).Lines.length)).toBe(1);
});
test('temporary API failure preserves bag and retry places a real order',async({page,audit})=>{
 await home(page);await add(page);await bag(page);await guestForm(page);await fillGuest(page);const before=await page.evaluate(()=>localStorage.getItem('shopqevora_guest_cart_v1'));
 audit.allow('/api/guest-orders',503);await page.route('**/api/guest-orders',route=>route.fulfill({status:503,contentType:'application/json',body:JSON.stringify({message:'Temporary failure. Your bag is preserved.'})}));
 await page.getByRole('button',{name:'Place guest demo order'}).click();await expect(page.getByRole('alert')).toContainText('bag is preserved');expect(await page.evaluate(()=>localStorage.getItem('shopqevora_guest_cart_v1'))).toBe(before);
 await page.unroute('**/api/guest-orders');await page.getByRole('button',{name:'Place guest demo order'}).click();await expect(page.locator('.order-reference')).toHaveText(/^GQ-[A-F0-9]{32}$/);
});
test('real API rejects invalid guest inputs and ignores forged prices',async({page})=>{
 for(const mutate of [(r:any)=>r.contact=null,(r:any)=>r.contact.email='invalid',(r:any)=>r.lines=[{productID:2147483647,quantity:1}],(r:any)=>r.lines[0].quantity=0,(r:any)=>r.lines[0].quantity=100]){const body=guestRequest();mutate(body);expect((await page.request.post(API+'/api/guest-orders',{data:body})).status()).toBe(400);}
 const body:any=guestRequest();body.total=.01;body.lines[0].unitPrice=.01;const response=await page.request.post(API+'/api/guest-orders',{data:body});expect(response.status()).toBe(200);expect((await response.json()).total).toBe(258);body.lines[0].quantity=3;expect((await page.request.post(API+'/api/guest-orders',{data:body})).status()).toBe(409);
});

test('concurrent duplicate POSTs create one private guest receipt',async({page})=>{
 const request=guestRequest();const responses=await Promise.all([page.request.post(API+'/api/guest-orders',{data:request}),page.request.post(API+'/api/guest-orders',{data:request})]);
 expect(responses.map(r=>r.status())).toEqual([200,200]);const receipts=await Promise.all(responses.map(r=>r.json()));expect(receipts[0].reference).toBe(receipts[1].reference);expect(receipts[0].total).toBe(258);
});
