document.addEventListener('keydown',event=>{
  if((event.ctrlKey||event.metaKey)&&!event.altKey&&!event.shiftKey&&event.key.toLowerCase()==='k'){
    const button=document.querySelector('[data-command-search]');
    if(button&&!button.disabled){event.preventDefault();button.click();}
  }
});

let fleetNavRef=null;
const fleetSystemTheme=matchMedia('(prefers-color-scheme: dark)');
function fleetModuleFromUrl(){return new URL(location.href).searchParams.get('module');}
function fleetPop(){if(fleetNavRef)fleetNavRef.invokeMethodAsync('RestoreModule',fleetModuleFromUrl()||'Overview').catch(()=>{});}
function fleetTheme(){if(fleetNavRef)fleetNavRef.invokeMethodAsync('SystemThemeChanged',fleetSystemTheme.matches).catch(()=>{});}
window.fleetPrefersDark=()=>fleetSystemTheme.matches;
window.fleetInitNavigation=ref=>{window.fleetDisposeNavigation();fleetNavRef=ref;addEventListener('popstate',fleetPop);fleetSystemTheme.addEventListener('change',fleetTheme);return fleetModuleFromUrl();};
window.fleetSetModule=module=>{const url=new URL(location.href);if(module==='Overview')url.searchParams.delete('module');else url.searchParams.set('module',module);if(url.href!==location.href)history.pushState(null,'',url);};
window.fleetDisposeNavigation=()=>{removeEventListener('popstate',fleetPop);fleetSystemTheme.removeEventListener('change',fleetTheme);fleetNavRef=null;};

// Consume a one-time language handoff while preserving module and other URL state.
window.fleetConsumeLanguage=()=>{
  const url=new URL(location.href),lang=url.searchParams.get('lang');
  if(lang!=='en'&&lang!=='ru')return null;
  url.searchParams.delete('lang');
  history.replaceState(history.state,'',url);
  return lang;
};


document.addEventListener('keydown',event=>{
  if(event.defaultPrevented||event.altKey||event.ctrlKey||event.metaKey||event.shiftKey)return;
  const nav=document.getElementById('workspace-nav');
  const input=nav?.querySelector('.nav-search input');
  if(!input?.value.trim()||!nav.contains(document.activeElement))return;
  const results=[...nav.querySelectorAll('.v4-search-actions button,.nav-group button')].filter(button=>!button.disabled&&button.getClientRects().length);
  const items=[input,...results],current=items.indexOf(document.activeElement);
  if(current<0)return;
  if(event.key==='ArrowDown'||event.key==='ArrowUp'){
    if(results.length===0)return;
    event.preventDefault();
    const step=event.key==='ArrowDown'?1:-1;
    items[(current+step+items.length)%items.length].focus();
  }else if(event.key==='Escape'&&current>0){
    event.preventDefault();input.value='';input.dispatchEvent(new Event('input',{bubbles:true}));input.focus();
  }
});


// Explicit boolean result across JS interop. Resolve Storage.setItem at call time;
// .NET can cache a previously bound function when using localStorage.setItem directly.
window.fleetTrySaveSnapshot=(key,json)=>{
  if(key!=='fleetmanagement.demo.v1'||typeof json!=='string')return false;
  try{localStorage.setItem(key,json);return true;}catch{return false;}
};
