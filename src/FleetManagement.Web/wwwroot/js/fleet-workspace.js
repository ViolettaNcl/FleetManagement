document.addEventListener('keydown',event=>{
  if((event.ctrlKey||event.metaKey)&&!event.altKey&&!event.shiftKey&&event.key.toLowerCase()==='k'){
    const button=document.querySelector('[data-command-search]');
    if(button&&!button.disabled){event.preventDefault();button.click();}
  }
});
