import {defineConfig,devices} from '@playwright/test';
import {execFileSync} from 'node:child_process';
import fs from 'node:fs';
// Firefox keeps FontFaceSet.ready pending while a deferred startup request is held.
// Screenshot helpers explicitly await fonts; avoid Playwright's duplicate document-load wait.
process.env.PW_TEST_SCREENSHOT_NO_FONTS_READY='1';
if(!process.env.SHOPQEVORA_E2E_RUN_ID){
 process.env.SHOPQEVORA_E2E_OWNER_PID=String(process.pid);
 process.stdout.write(execFileSync('dotnet',['run','--project','tools/ShopQevora.E2E','--','prepare'],{encoding:'utf8'}));
 process.env.SHOPQEVORA_E2E_RUN_ID=JSON.parse(fs.readFileSync('.e2e/runtime.json','utf8')).RunId;
}
export default defineConfig({
 testDir:'./e2e',testMatch:'**/*.spec.ts',fullyParallel:false,workers:2,timeout:60000,expect:{timeout:10000},retries:0,
 globalTeardown:'./e2e/global-teardown.ts',outputDir:'test-output/results',
 reporter:[['list'],['html',{outputFolder:'test-output/report',open:'never'}],['json',{outputFile:'test-output/results.json'}]],
 use:{baseURL:'http://localhost:5174',locale:'en-US',timezoneId:'Asia/Karachi',reducedMotion:'no-preference',trace:'retain-on-failure',screenshot:'only-on-failure',video:'retain-on-failure'},
 webServer:[
  {command:'node e2e/start-server.mjs api',url:'http://localhost:5101/api/health',reuseExistingServer:false,timeout:120000,stdout:'ignore',stderr:'pipe',gracefulShutdown:{signal:'SIGINT',timeout:10000}},
  {command:'node e2e/start-server.mjs client',url:'http://localhost:5174',reuseExistingServer:false,timeout:120000,stdout:'ignore',stderr:'pipe',gracefulShutdown:{signal:'SIGINT',timeout:10000}}
 ],
 projects:[
  {name:'chromium',use:{...devices['Desktop Chrome'],viewport:{width:1440,height:900}}},
  {name:'firefox',use:{...devices['Desktop Firefox'],viewport:{width:1440,height:900}}},
  {name:'webkit',use:{...devices['Desktop Safari'],viewport:{width:1440,height:900}}},
  {name:'chromium-touch',testMatch:/(cart|guest-checkout|navigation|accessibility|profiles|enhancements|contact)\.spec\.ts/,use:{...devices['iPhone 13'],defaultBrowserType:'chromium'}},
  {name:'webkit-touch',testMatch:/(cart|guest-checkout|navigation|accessibility|profiles|enhancements|contact)\.spec\.ts/,use:{...devices['iPhone 13'],defaultBrowserType:'webkit'}}
 ]
});
