import type {Page,APIRequestContext} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {test,expect,API,email,account,login,noOverflow,shot} from './helpers';

async function openContact(page:Page,path='/contact'){
 await page.goto(path);await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.getByRole('heading',{name:'Contact Us',exact:true})).toBeVisible();await expect(page.getByRole('button',{name:'Save demo inquiry'})).toBeEnabled();
}
async function fill(page:Page,address=email()){
 await page.getByRole('textbox',{name:'Full name',exact:true}).fill('Fictional Contact Customer');await page.getByRole('textbox',{name:'Email address',exact:true}).fill(address);await page.getByLabel(/International phone/).fill('+92 300 1234567');await page.getByRole('combobox',{name:'Inquiry category',exact:true}).selectOption('order');await page.getByLabel(/Order reference/).fill('GQ-'+'A'.repeat(32));await page.getByRole('textbox',{name:'Message',exact:true}).fill('A fictional inquiry about the ShopQevora demonstration catalog.');return address;
}
async function payload(request:APIRequestContext){const challenge=await(await request.get(API+'/api/contact/challenge')).json();return{...challenge,fullName:'Fictional API Contact',email:email(),phone:'+12025550123',category:'feedback',orderReference:'',message:'A fictional API inquiry for isolated browser verification.',website:''};}

test('guest contact validation, successful save, idempotent retry and private confirmation',async({page})=>{
 await openContact(page);await expect(page.locator('#contact-demo')).toContainText('not actively monitored');await expect(page.locator('#contact-demo')).toContainText('No emails are sent');
 await page.getByRole('button',{name:'Save demo inquiry'}).click();expect(await page.locator('#contact-name').evaluate((e:HTMLInputElement)=>e.validity.valueMissing)).toBe(true);
 const address=await fill(page);await page.locator('#contact-email').fill('invalid-email');await page.getByRole('button',{name:'Save demo inquiry'}).click();expect(await page.locator('#contact-email').evaluate((e:HTMLInputElement)=>e.validity.valid)).toBe(false);await page.locator('#contact-email').fill(address);
 await page.locator('#contact-phone').fill('03001234567');await page.getByRole('button',{name:'Save demo inquiry'}).click();await expect(page.locator('.validation-errors')).toContainText('international format');await page.locator('#contact-phone').fill('+92 300 1234567');
 let sent:any;page.on('request',r=>{if(r.url().endsWith('/api/contact/inquiries')&&r.method()==='POST')sent=r.postDataJSON();});await shot(page,'contact-form');await page.getByRole('button',{name:'Save demo inquiry'}).click();await expect(page.locator('.contact-reference')).toHaveText(/^CQ-[A-F0-9]{32}$/);await expect(page.getByRole('status')).toContainText('No email was sent');await expect(page.locator('.contact-confirmation')).toBeFocused();
 const reference=await page.locator('.contact-reference').innerText();expect(sent).toBeTruthy();const retry=await page.request.post(API+'/api/contact/inquiries',{data:sent});expect(retry.status()).toBe(200);expect((await retry.json()).reference).toBe(reference);expect((await page.request.get(API+'/api/contact/inquiries/'+reference)).status()).toBe(404);
 expect(await page.evaluate(()=>JSON.stringify({...localStorage,...sessionStorage}))).not.toContain('Fictional Contact Customer');await shot(page,'contact-confirmation');await page.getByRole('button',{name:'Send another inquiry'}).click();await expect(page.locator('#contact-message')).toHaveValue('');
});

test('signed-in customers can send inquiries and leave optional phone and order blank',async({page,request})=>{
 const user=await account(request);await login(page,user);await openContact(page);await expect(page.locator('#contact-email')).toHaveValue(user.email);await page.locator('#contact-name').fill('Fictional Signed-In Customer');await page.locator('#contact-message').fill('A fictional question from a signed-in customer.');await page.locator('#contact-category').selectOption('account');await page.getByRole('button',{name:'Save demo inquiry'}).click();await expect(page.locator('.contact-reference')).toHaveText(/^CQ-[A-F0-9]{32}$/);await expect(page.getByRole('status')).toContainText('saved successfully');
});

test('network failures preserve fields; refreshing keeps the retry key; spam and private reads are rejected',async({page,request,audit})=>{
 await openContact(page);await fill(page);let first:any;page.on('request',r=>{if(r.url().endsWith('/api/contact/inquiries')&&r.method()==='POST')first??=r.postDataJSON();});audit.allow('/api/contact/inquiries',503);await page.route('**/api/contact/inquiries',r=>r.fulfill({status:503,contentType:'application/json',body:JSON.stringify({message:'Temporary failure. Your message is preserved.'})}));await page.getByRole('button',{name:'Save demo inquiry'}).click();await expect(page.getByRole('alert')).toContainText('message is preserved');await expect(page.locator('#contact-message')).toHaveValue(/fictional inquiry/);await page.unroute('**/api/contact/inquiries');await page.getByRole('button',{name:'Refresh form session'}).click();await expect(page.getByRole('button',{name:'Save demo inquiry'})).toBeEnabled();
 let retry:any;page.on('request',r=>{if(r.url().endsWith('/api/contact/inquiries')&&r.method()==='POST')retry=r.postDataJSON();});await page.getByRole('button',{name:'Save demo inquiry'}).click();await expect(page.locator('.contact-reference')).toBeVisible();expect(retry.submissionKey).toBe(first.submissionKey);
 const input=await payload(request);const spam=await request.post(API+'/api/contact/inquiries',{data:{...input,website:'https://spam.example'}});expect(spam.status()).toBe(400);expect((await request.post(API+'/api/contact/inquiries',{data:{...input,formToken:'tampered'}})).status()).toBe(400);expect((await request.post(API+'/api/contact/inquiries',{data:{...input,message:'x'.repeat(4001)}})).status()).toBe(400);
 const responses=await Promise.all([request.post(API+'/api/contact/inquiries',{data:input}),request.post(API+'/api/contact/inquiries',{data:input})]);expect(responses.map(r=>r.status()).sort()).toEqual([200,201]);const receipts=await Promise.all(responses.map(r=>r.json()));expect(receipts[0].reference).toBe(receipts[1].reference);expect(JSON.stringify(receipts)).not.toContain(input.email);
 const other=await account(request);for(const path of ['/api/contact/inquiries','/api/contact/inquiries/'+receipts[0].reference]){const response=await request.get(API+path,{headers:{Authorization:'Bearer '+other.token}});expect(response.ok()).toBe(false);expect(await response.text()).not.toContain(input.message);}
});

test('Help & Support, footer and legacy contact links open the form; FAQ and keyboard access work',async({page})=>{
 await page.goto('/');await expect(page.locator('#startup-screen')).toHaveCount(0);await page.getByRole('button',{name:'Open menu'}).click();await expect(page.getByRole('heading',{name:'Help & Support',exact:true})).toBeVisible();await page.getByRole('dialog').getByRole('link',{name:'Contact Us',exact:true}).click();await expect(page).toHaveURL(/\/contact$/);await expect(page.getByRole('button',{name:'Save demo inquiry'})).toBeEnabled();await page.locator('footer').getByRole('link',{name:'Contact Us',exact:true}).click();await expect(page.getByRole('heading',{name:'Contact Us',exact:true})).toBeVisible();
 await openContact(page,'/information/contact');await page.locator('summary').first().focus();await page.keyboard.press('Enter');await expect(page.locator('details').first()).toHaveAttribute('open','');await expect(page.locator('details').first()).toContainText('No email delivery service');
 await page.emulateMedia({reducedMotion:'reduce'});const scan=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa','wcag22aa']).analyze();expect(scan.violations).toEqual([]);
});

test('contact form fits all existing responsive viewports',async({page})=>{
 await openContact(page);await page.emulateMedia({reducedMotion:'reduce'});
 for(const [width,height] of [[320,568],[375,667],[390,844],[430,932],[412,915],[600,960],[768,1024],[820,1180],[1024,1366],[1280,800],[1366,768],[1440,900],[1920,1080],[2560,1440]]){
  await page.setViewportSize({width,height});await noOverflow(page);await page.locator('#contact-message').scrollIntoViewIfNeeded();const box=await page.locator('#contact-message').boundingBox();expect(box!.x).toBeGreaterThanOrEqual(0);expect(box!.x+box!.width).toBeLessThanOrEqual(width);await page.getByRole('button',{name:'Save demo inquiry'}).scrollIntoViewIfNeeded();await expect(page.getByRole('button',{name:'Save demo inquiry'})).toBeVisible();await shot(page,'contact-'+width);
 }
});
