import fs from 'node:fs';import {spawn} from 'node:child_process';
const state=JSON.parse(fs.readFileSync('.e2e/runtime.json','utf8'));
const api=process.argv[2]==='api';
const child=spawn('dotnet',['run','--no-build','--project',api?'src/ShopQevora.Api':'src/ShopQevora.Client','--launch-profile',api?'ShopQevora.Api':'ShopQevora.Client','--urls',api?'http://localhost:5101':'http://localhost:5174'],{stdio:'inherit',env:{...process.env,...(api?{ClientOrigin:"http://localhost:5174",ConnectionStrings__Shop:state.ConnectionString,Jwt__Key:state.SigningKey,E2E__InstanceId:state.RunId,GuestRateLimit__PermitLimit:'1000',ContactRateLimit__PermitLimit:'1000',ContactRateLimit__ChallengeLimit:'1000',Logging__LogLevel__Default:'Warning','Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command':'Warning'}:{})}});
for(const signal of ['SIGINT','SIGTERM'])process.on(signal,()=>child.kill(signal));
child.on('exit',code=>process.exit(code??1));
