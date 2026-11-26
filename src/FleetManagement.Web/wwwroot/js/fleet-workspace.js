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
