import {execFileSync} from 'node:child_process';
export default async function teardown(){process.stdout.write(execFileSync('dotnet',['run','--no-build','--project','tools/ShopQevora.E2E','--','cleanup'],{encoding:'utf8'}));}
