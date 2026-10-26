import {test,expect,home,add,bag,guestForm,fillGuest,noOverflow,shot} from './helpers';
const viewports=[[320,568],[375,667],[390,844],[430,932],[412,915],[600,960],[768,1024],[820,1180],[1024,1366],[1280,800],[1366,768],[1440,900],[1920,1080],[2560,1440]];
for(const [width,height] of viewports)test(`layout ${width}x${height}: home, cart, checkout and footer`,async({page})=>{
 await page.setViewportSize({width,height});await page.emulateMedia({reducedMotion:'reduce'});await home(page);await noOverflow(page);const hero=await page.locator('.hero-carousel').boundingBox();expect(Math.round(hero!.width)).toBe(width);
 const logo=await page.locator('header .brand img').boundingBox();expect(logo!.x).toBeGreaterThanOrEqual(0);expect(logo!.x+logo!.width).toBeLessThanOrEqual(width);
 await shot(page,`${width}-home`);await page.locator('#collections').scrollIntoViewIfNeeded();await shot(page,`${width}-categories`);await page.locator('#collection').scrollIntoViewIfNeeded();await noOverflow(page);await shot(page,`${width}-products`);
 await add(page);await bag(page);await noOverflow(page);await shot(page,`${width}-cart`);await guestForm(page);await fillGuest(page);await noOverflow(page);await expect(page.getByRole('button',{name:'Place guest demo order'})).toBeEnabled();await shot(page,`${width}-checkout`);await page.locator('footer').scrollIntoViewIfNeeded();await noOverflow(page);await shot(page,`${width}-footer`);
});
