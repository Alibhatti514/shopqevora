import {test as base,expect,type Page,type APIRequestContext} from '@playwright/test';
import {randomUUID,randomBytes} from 'node:crypto';
export {expect};export const API='http://localhost:5101';
export const runId=process.env.SHOPQEVORA_E2E_RUN_ID!;
export const email=()=>`e2e-${runId}-${randomUUID()}@example.test`;
export type Audit={allow:(path:string,status:number)=>void;allowFailure:(path:string)=>void};
export const test=base.extend<{audit:Audit}>({
 audit:[async({page},use,testInfo)=>{
  const rules:{path:string,status:number}[]=[],allowedFailure:string[]=[],issues:string[]=[],observations:string[]=[];
  const expected=(url:string,status:number)=>rules.some(r=>url.includes(r.path)&&status===r.status);
  page.on('response',response=>{if(response.status()>=400){const text=`HTTP ${response.status()} ${response.url()}`;if(expected(response.url(),response.status()))observations.push(text);else issues.push(text);}});
  page.on('requestfailed',request=>{const error=request.failure()?.errorText??'';if(allowedFailure.some(p=>request.url().includes(p))||/ERR_ABORTED|NS_BINDING_ABORTED|cancelled/i.test(error))observations.push(`Cancelled/expected: ${request.url()} ${error}`);else issues.push(`Request failed: ${request.url()} ${error}`);});
  page.on('pageerror',error=>issues.push(error.message));
  page.on('console',message=>{if(message.type()!=='error')return;const url=message.location().url;const text=message.text();if(rules.some(r=>(url.includes(r.path)||!url)&&text.includes(String(r.status))))observations.push(text);else issues.push(`Console ${text} (${url})`);});
  await page.route('http://localhost:5100/api/**',route=>route.continue({url:route.request().url().replace('localhost:5100','localhost:5101')}));
  const health=await page.request.get(API+'/api/health');expect((await health.json()).instanceId,'Refuse to mutate an unowned API instance').toBe(runId);
  await use({allow:(path,status)=>rules.push({path,status}),allowFailure:path=>allowedFailure.push(path)});
  await testInfo.attach('execution-context',{body:JSON.stringify({engine:page.context().browser()?.browserType().name(),viewport:page.viewportSize(),project:testInfo.project.name}),contentType:'application/json'});
  if(issues.length||observations.length)await testInfo.attach('browser-network-audit',{body:JSON.stringify({issues,expected:observations},null,2),contentType:'application/json'});
  expect(issues,'Unexpected browser/HTTP failures').toEqual([]);
 },{auto:true}]
});
export async function home(page:Page){await page.goto('/');await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('#collection .product-card')).toHaveCount(36);await page.evaluate(()=>document.fonts.ready);}
export async function stable(page:Page){
 await page.emulateMedia({reducedMotion:'reduce'});await page.evaluate(()=>document.fonts.ready);
 await page.evaluate(()=>{for(const image of document.images){const r=image.getBoundingClientRect();if(r.bottom>0&&r.top<innerHeight)image.loading='eager';}});
 await expect.poll(()=>page.evaluate(async()=>{
  const images=[...document.images].filter(i=>{const r=i.getBoundingClientRect();return r.bottom>0&&r.top<innerHeight;});
  const ready=await Promise.all(images.map(async image=>{if(!image.complete||!image.naturalWidth)return false;try{await image.decode();return true;}catch{return false;}}));
  return ready.every(Boolean);
 }),{message:'Every visible image must successfully load and decode before capture'}).toBe(true);
}
export async function shot(page:Page,name:string){await stable(page);const path=test.info().outputPath(name+'.png');await page.screenshot({path,fullPage:false,animations:'disabled'});await test.info().attach(name,{path,contentType:'image/png'});}
export async function add(page:Page,name='Studio Headphones'){await page.getByRole('button',{name:`Add ${name} to your bag`,exact:true}).first().click();await expect(page.locator('.cart-toast')).toContainText(name);}
export async function bag(page:Page){await page.goto('/cart');await expect(page.locator('#startup-screen')).toHaveCount(0);await expect(page.locator('.cart-row').first()).toBeVisible();}
export async function guestForm(page:Page){await page.getByRole('button',{name:'Continue to checkout'}).click();await page.getByRole('button',{name:'Continue as Guest',exact:true}).click();await expect(page.locator('#guest-name')).toBeVisible();}
export async function fillGuest(page:Page,contactEmail=email()){
 for(const [id,value] of [['guest-name','E2E Fictional Buyer'],['guest-email',contactEmail],['guest-phone','03001234567'],['guest-address','House 12, Fictional Test Street'],['guest-city','Karachi'],['guest-region','Sindh'],['guest-postal','74000']])await page.locator('#'+id).fill(value);
 return contactEmail;
}
export function contact(contactEmail=email()){return{fullName:'E2E Fictional Buyer',email:contactEmail,phone:'03001234567',address:'House 12, Fictional Test Street',city:'Karachi',region:'Sindh',postalCode:'74000',country:'PK'};}
export function guestRequest(){return{checkoutKey:randomUUID().replaceAll('-',''),contact:contact(),lines:[{productID:1,quantity:2}]};}
export async function account(request:APIRequestContext){const credentials={email:email(),password:'Qq7!'+randomBytes(20).toString('hex')};expect((await request.post(API+'/api/auth/register',{data:credentials})).status()).toBe(200);const result=await request.post(API+'/api/auth/login',{data:credentials});expect(result.status()).toBe(200);return{...credentials,token:(await result.json()).token as string};}
export async function login(page:Page,credentials:{email:string,password:string}){await page.goto('/login');await expect(page.locator('#startup-screen')).toHaveCount(0);await page.getByLabel('Email',{exact:true}).fill(credentials.email);await page.getByLabel('Password',{exact:true}).fill(credentials.password);await page.getByRole('button',{name:'Sign in',exact:true}).click();await expect(page).toHaveURL(/\/cart$/);}
export async function noOverflow(page:Page){expect(await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,viewport:innerWidth}))).toEqual(expect.objectContaining({scroll:await page.evaluate(()=>innerWidth)}));}
