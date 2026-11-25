"use strict";
// Narrow SignalR JSON / Long Polling subscriber for FleetChanged only.
// No generic hub method invocation or browser bearer in a URL.
function startFleetRealtime({bearer,onState,onChanged,onUnauthorized}){
    const controller=new AbortController(), signal=controller.signal;
    let connection=null,retry=null,ping=null,attempt=0,stopped=false;
    const headers={"Authorization":"Bearer "+bearer};
    function active(){return !stopped&&!signal.aborted;}
    async function send(path,method="GET",body,timeout=30000){
        const response=await fetch(path,{method,headers:body===undefined?headers:{...headers,"Content-Type":"text/plain;charset=UTF-8"},
            body,cache:"no-store",credentials:"omit",redirect:"error",signal:AbortSignal.any([signal,AbortSignal.timeout(timeout)])});
        if(response.status===401){if(active())onUnauthorized();throw Error("Unauthorized");}
        if(!response.ok||response.status===204)throw Error("Hub transport closed");
        return response;
    }
    async function disconnect(path){
        if(!path)return;
        try{await fetch(path,{method:"DELETE",headers,credentials:"omit",cache:"no-store",redirect:"error",signal:AbortSignal.timeout(3000)});}catch{}
    }
    async function run(){
        let ownPath=null,ready=false;
        try{
            if(!active())return;
            onState("SignalR connecting…");
            const response=await send("/hubs/fleet/negotiate?negotiateVersion=1","POST");
            const data=await response.json();
            if(!active())return;
            if(typeof data.connectionToken!=="string"||data.url||!data.availableTransports?.some(t=>t.transport==="LongPolling"&&t.transferFormats.includes("Text")))throw Error("Unsupported negotiation");
            ownPath="/hubs/fleet?id="+encodeURIComponent(data.connectionToken);connection=ownPath;
            await (await send(ownPath)).text(); // establish Long Polling transport
            await send(ownPath,"POST",JSON.stringify({protocol:"json",version:1})+"\x1e");
            let buffered="";
            while(active()){
                const response=await send(ownPath);
                buffered+=await response.text();
                if(buffered.length>65536)throw Error("Hub frame too large");
                const frames=buffered.split("\x1e");buffered=frames.pop();
                for(const frame of frames){
                    if(!frame)continue;
                    const item=JSON.parse(frame);
                    if(!ready){
                        if(item.error||item.type!==undefined)throw Error("Hub handshake rejected");
                        ready=true;attempt=0;onState("SignalR connected");
                        ping=setInterval(()=>{if(active())send(ownPath,"POST",'{"type":6}'+"\x1e",10000).catch(()=>{});},15000);
                    }else if(item.type===1&&item.target==="FleetChanged"){onChanged();}
                    else if(item.type===7)throw Error("Hub closed");
                }
            }
        }catch{
            if(active())onState("SignalR unavailable · revision polling remains active");
        }finally{
            if(ping!==null)clearInterval(ping);ping=null;
            if(connection===ownPath)connection=null;
            await disconnect(ownPath);
            if(active()){const delay=Math.min(30000,1000*2**Math.min(attempt++,5));retry=setTimeout(run,delay);}
        }
    }
    run();
    return ()=>{stopped=true;controller.abort();if(retry!==null)clearTimeout(retry);if(ping!==null)clearInterval(ping);};
}
