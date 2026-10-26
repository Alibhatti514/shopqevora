import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';

const source=await fs.readFile(new URL('../../src/ShopQevora.Client/wwwroot/js/hero-carousel.js',import.meta.url),'utf8');
const carousel=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));

function harness() {
    let nextId=0;
    const intervals=new Map(),rootEvents=new Map(),documentEvents=new Map(),motionEvents=new Map(),calls=[];
    const motion={matches:false,addEventListener:(key,fn)=>motionEvents.set(key,fn),removeEventListener:key=>motionEvents.delete(key)};
    const focused={};
    const root={matches:()=>false,contains:node=>node===focused,querySelectorAll:()=>[{decode:()=>Promise.resolve()},{decode:()=>Promise.resolve()}],addEventListener:(key,fn)=>rootEvents.set(key,fn),removeEventListener:key=>rootEvents.delete(key)};
    globalThis.window={matchMedia:()=>motion,setInterval:(fn,delay)=>{assert.equal(delay,2000);intervals.set(++nextId,fn);return nextId;},clearInterval:id=>intervals.delete(id)};
    globalThis.document={hidden:false,activeElement:null,addEventListener:(key,fn)=>documentEvents.set(key,fn),removeEventListener:key=>documentEvents.delete(key)};
    const dotnet={invokeMethodAsync:async(...args)=>{calls.push(args);}};
    return {root,rootEvents,documentEvents,motionEvents,motion,intervals,calls,focused,dotnet};
}
const settle=()=>new Promise(resolve=>setImmediate(resolve));

test('autoplay ticks every 2000ms and pauses for hover, focus, visibility and user pause',async()=>{
    const h=harness();carousel.attach(h.root,h.dotnet);await settle();assert.equal(h.intervals.size,1);
    await [...h.intervals.values()][0]();assert.ok(h.calls.some(c=>c[0]==='Advance'));
    h.rootEvents.get('mouseenter')();assert.equal(h.intervals.size,0);
    h.rootEvents.get('mouseleave')();assert.equal(h.intervals.size,1);
    h.rootEvents.get('focusin')();assert.equal(h.intervals.size,0);
    h.rootEvents.get('focusout')({relatedTarget:h.focused});assert.equal(h.intervals.size,0);
    h.rootEvents.get('focusout')({relatedTarget:null});assert.equal(h.intervals.size,1);
    carousel.setPaused(h.root,true);assert.equal(h.intervals.size,0);
    carousel.setPaused(h.root,false);assert.equal(h.intervals.size,1);
    document.hidden=true;h.documentEvents.get('visibilitychange')();assert.equal(h.intervals.size,0);
    document.hidden=false;h.documentEvents.get('visibilitychange')();assert.equal(h.intervals.size,1);
    carousel.detach(h.root);assert.equal(h.intervals.size,0);assert.equal(h.rootEvents.size,0);assert.equal(h.documentEvents.size,0);assert.equal(h.motionEvents.size,0);
});

test('reduced-motion disables autoplay; reattachment never leaks timers or listeners',async()=>{
    const h=harness();h.motion.matches=true;carousel.attach(h.root,h.dotnet);await settle();assert.equal(h.intervals.size,0);
    assert.ok(h.calls.some(c=>c[0]==='MotionChanged'&&c[1]===true));
    h.motion.matches=false;h.motionEvents.get('change')();assert.equal(h.intervals.size,1);
    carousel.attach(h.root,h.dotnet);await settle();assert.equal(h.intervals.size,1);assert.equal(h.rootEvents.size,4);
    carousel.detach(h.root);carousel.detach(h.root);assert.equal(h.intervals.size,0);assert.equal(h.rootEvents.size,0);
});

test('autoplay waits for images to decode and remains stopped if an image fails',async()=>{
    const h=harness();let release;
    h.root.querySelectorAll=()=>[{decode:()=>new Promise(resolve=>release=resolve)}];
    carousel.attach(h.root,h.dotnet);await settle();assert.equal(h.intervals.size,0);
    release();await settle();assert.equal(h.intervals.size,1);carousel.detach(h.root);
    h.root.querySelectorAll=()=>[{decode:()=>Promise.reject(new Error('missing image'))}];
    carousel.attach(h.root,h.dotnet);await settle();assert.equal(h.intervals.size,0);carousel.detach(h.root);
});

test('a late image decode after disposal does not restart autoplay',async()=>{
    const h=harness();let release;h.root.querySelectorAll=()=>[{decode:()=>new Promise(resolve=>release=resolve)}];
    carousel.attach(h.root,h.dotnet);carousel.detach(h.root);release();await settle();assert.equal(h.intervals.size,0);
});
