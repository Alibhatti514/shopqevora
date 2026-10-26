let active;
export function open(element, trigger) {
 close(); const previous=trigger??document.activeElement; const oldOverflow=document.body.style.overflow;
 const controls=()=>[...element.querySelectorAll('a[href],button,input,select')].filter(x=>!x.disabled&&x.getClientRects().length);
 const key=e=>{if(e.key==='Tab'){const items=controls();if(!items.length)return;if(e.shiftKey&&document.activeElement===items[0]){e.preventDefault();items.at(-1).focus();}else if(!e.shiftKey&&document.activeElement===items.at(-1)){e.preventDefault();items[0].focus();}}};
 const background=[];
 for(let branch=element;branch?.parentElement;branch=branch.parentElement){
  for(const node of branch.parentElement.children){
   if(node!==branch&&!node.classList.contains('drawer-backdrop')&&!['SCRIPT','STYLE','LINK'].includes(node.tagName))background.push({node,inert:node.inert});
  }
  if(branch.parentElement===document.body)break;
 }
 background.forEach(x=>x.node.inert=true);

 document.body.style.overflow='hidden';element.addEventListener('keydown',key);controls()[0]?.focus();
 active={element,key,previous,oldOverflow,trigger,background};
}
export function close(){if(!active)return;const a=active;active=null;a.element.removeEventListener('keydown',a.key);document.body.style.overflow=a.oldOverflow;a.background.forEach(x=>x.node.inert=x.inert);if(a.previous?.isConnected)a.previous.focus();}
