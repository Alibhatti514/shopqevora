import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
const source=await fs.readFile(new URL('../../src/ShopQevora.Client/wwwroot/js/scroll-motion.js',import.meta.url),'utf8');
const motion=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
function element({hero=false,card=false}={}) {
 const classes=new Set();return {dataset:{},classes,textContent:'Original accessible text',classList:{add:x=>classes.add(x),remove:x=>classes.delete(x)},closest:()=>hero?{}:null,querySelector:()=>null,matches:()=>card,getBoundingClientRect:()=>({top:1200,bottom:1300})};
}
function harness(reduced=false) {
 const intersections=[],mutations=[],listeners=new Map(),mediaListeners=new Map(),frames=new Map();let nextFrame=0;
 class IO {constructor(callback,options){this.callback=callback;this.options=options;this.nodes=new Set();intersections.push(this);}observe(node){this.nodes.add(node);}unobserve(node){this.nodes.delete(node);}disconnect(){this.nodes.clear();}}
 class MO {constructor(callback){this.callback=callback;this.active=false;mutations.push(this);}observe(){this.active=true;}disconnect(){this.active=false;}}
 globalThis.IntersectionObserver=IO;globalThis.MutationObserver=MO;
 const media={matches:reduced,addEventListener:(key,fn)=>mediaListeners.set(key,fn),removeEventListener:key=>mediaListeners.delete(key)};
 globalThis.window={scrollY:0,innerHeight:1000,matchMedia:()=>media,addEventListener:(key,fn)=>listeners.set(key,fn),removeEventListener:key=>listeners.delete(key),requestAnimationFrame:fn=>{frames.set(++nextFrame,fn);return nextFrame;},cancelAnimationFrame:id=>frames.delete(id)};
 const node=element(),hero=element({hero:true}),nodes=[node,hero],root={querySelectorAll:()=>nodes,contains:el=>nodes.includes(el)};
 const scroll=y=>{window.scrollY=y;listeners.get('scroll')();for(const [id,fn] of [...frames]){frames.delete(id);fn();}};
 return {root,node,hero,nodes,intersections,mutations,listeners,mediaListeners,media,frames,scroll};
}
function emit(h,intersect,rect,ratio=0.5) {h.intersections.at(-1).callback([{target:h.node,isIntersecting:intersect,intersectionRatio:ratio,boundingClientRect:rect}]);}

test('initializes offscreen headings and reveals without touching hero or text content',()=>{
 const h=harness();motion.attach(h.root);assert.equal(h.node.dataset.motionState,'rest');assert.equal(h.intersections[0].nodes.size,1);assert.ok(!h.hero.classes.has('motion-enhanced'));assert.equal(h.node.textContent,'Original accessible text');
 emit(h,true,{top:100,bottom:160});assert.equal(h.node.dataset.motionState,'shown');motion.detach(h.root);assert.equal(h.node.dataset.motionState,undefined);
});
test('reverse scrolling replays only after fully exiting, never flashes partially visible text',()=>{
 const h=harness();motion.attach(h.root);emit(h,true,{top:100,bottom:160});h.scroll(500);emit(h,false,{top:-50,bottom:10},0);assert.equal(h.node.dataset.motionState,'shown');emit(h,false,{top:-80,bottom:-1},0);assert.equal(h.node.dataset.motionState,'rest');h.scroll(100);emit(h,true,{top:10,bottom:80});assert.equal(h.node.dataset.direction,'up');assert.equal(h.node.dataset.motionState,'shown');motion.detach(h.root);
});
test('reduced motion shows all content and reacts to preference changes',()=>{
 const h=harness(true);motion.attach(h.root);assert.equal(h.node.dataset.motionState,'shown');assert.ok(!h.node.classes.has('motion-enhanced'));assert.equal(h.intersections[0].nodes.size,0);
 h.media.matches=false;h.mediaListeners.get('change')();assert.equal(h.intersections[0].nodes.size,1);h.media.matches=true;h.mediaListeners.get('change')();assert.equal(h.intersections[0].nodes.size,0);assert.equal(h.node.dataset.motionState,'shown');motion.detach(h.root);
});
test('repeated mounting cleans observers/listeners and cancellation; handles dynamic Blazor cards',()=>{
 const h=harness();motion.attach(h.root);motion.attach(h.root);assert.equal(h.intersections[0].nodes.size,0);assert.equal(h.mutations[0].active,false);assert.equal(h.listeners.size,1);assert.equal(h.mediaListeners.size,1);
 const card=element({card:true});h.nodes.push(card);h.mutations.at(-1).callback();assert.ok(h.intersections.at(-1).nodes.has(card));h.nodes.splice(h.nodes.indexOf(card),1);h.mutations.at(-1).callback();assert.ok(!h.intersections.at(-1).nodes.has(card));
 h.listeners.get('scroll')();assert.equal(h.frames.size,1);motion.detach(h.root);motion.detach(h.root);assert.equal(h.frames.size,0);assert.equal(h.listeners.size,0);assert.equal(h.mediaListeners.size,0);assert.ok(h.mutations.every(x=>!x.active));
});
