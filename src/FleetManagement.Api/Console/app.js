"use strict";
const $=id=>document.getElementById(id);
let token=null, generation=0, busy=false, currentPage="fleet";
const editors=[$("work-form"),$("shift-form"),$("reservation-form")];
function status(text,error=false){$("status").textContent=text;$("status").classList.toggle("error",error);}
function signOut(message="Signed out."){
    token=null;generation++;busy=false;
    $("auth").hidden=false;$("workspace").hidden=true;$("logout").hidden=true;
    $("identity").textContent="";$("policy").textContent="";$("updated").textContent="";
    for(const id of ["vehicles","drivers","trips","work","shifts","reservations","audit"])$(id).replaceChildren();
    for(const id of ["vehicles-count","drivers-count","trips-count","work-count"])$(id).textContent="—";
    for(const form of editors){form.reset();form.hidden=true;for(const select of form.querySelectorAll("select[name=vehicleId],select[name=driverId]"))select.replaceChildren();}
    $("audit-tab").hidden=true;$("login").reset();status(message);
}
async function api(path,method="GET",body,authenticated=true){
    const headers={"Accept":"application/json"};
    if(authenticated){if(!token)throw Error("Sign in again.");headers.Authorization="Bearer "+token;}
    if(body!==undefined)headers["Content-Type"]="application/json";
    const response=await fetch(path,{method,headers,body:body===undefined?undefined:JSON.stringify(body),cache:"no-store",credentials:"omit",redirect:"error"});
    if(response.status===401){if(authenticated)signOut("Session expired. Sign in again.");throw Error("Authentication failed.");}
    if(!response.ok){let detail;try{detail=await response.json();}catch{}throw Error(typeof detail?.error==="string"?detail.error:"Request failed (HTTP "+response.status+").");}
    return response.status===204?null:await response.json();
}
function table(id,columns,rows){
    const root=$(id);root.replaceChildren();
    const head=document.createElement("thead"),tr=document.createElement("tr");
    for(const [label] of columns){const th=document.createElement("th");th.scope="col";th.textContent=label;tr.append(th);}
    head.append(tr);root.append(head);
    const body=document.createElement("tbody");
    for(const row of rows){const r=document.createElement("tr");for(const [,get] of columns){const td=document.createElement("td");const value=get(row);td.textContent=value==null?"—":String(value);r.append(td);}body.append(r);}
    if(!rows.length){const r=document.createElement("tr"),td=document.createElement("td");td.colSpan=columns.length;td.textContent="No records.";r.append(td);body.append(r);}
    root.append(body);
}
const date=x=>typeof x==="string"?x.replace("T"," ").replace(/Z$/," UTC"):"—";
const text=key=>row=>row[key];
function options(select,rows,label){const old=select.value;select.replaceChildren();const blank=document.createElement("option");blank.value="";blank.textContent="Select";select.append(blank);for(const r of rows){const o=document.createElement("option");o.value=String(r.id);o.textContent=label(r);select.append(o);}if([...select.options].some(o=>o.value===old))select.value=old;}
function tab(name){currentPage=name;for(const item of document.querySelectorAll("[data-page]"))item.hidden=item.dataset.page!==name;for(const button of document.querySelectorAll("[data-tab]"))button.setAttribute("aria-pressed",String(button.dataset.tab===name));}
async function refresh(){
    const active=generation;
    const [me,fleet,work,shifts,reservations,policy]=await Promise.all([api("/api/auth/me"),api("/api/fleet"),api("/api/operations/maintenance"),api("/api/operations/shifts"),api("/api/operations/reservations"),api("/api/operations/policy")]);
    if(active!==generation||!token)return;
    const admin=me.roles.includes("Admin"),canWrite=admin||me.roles.includes("Dispatcher");
    const audit=admin?await api("/api/admin/audit?limit=100"):[];
    if(active!==generation||!token)return;
    $("identity").textContent=me.name+" · "+me.roles.join(", ");
    $("policy").textContent=policy.requireShifts?"Strict driver shifts enabled":"Driver shifts advisory";
    const vehicles=fleet.vehicles,drivers=fleet.drivers;
    const vehicle=id=>vehicles.find(v=>v.id===id)?.plate??("#"+id);
    const driver=id=>drivers.find(d=>d.id===id)?.name??("#"+id);
    $("vehicles-count").textContent=vehicles.length;$("drivers-count").textContent=drivers.length;$("trips-count").textContent=fleet.trips.length;$("work-count").textContent=work.length;
    table("vehicles",[["Plate",text("plate")],["Model",text("model")],["Mileage (km)",text("mileage")],["Last service",r=>date(r.lastService)]],vehicles);
    table("drivers",[["Name",text("name")],["License",text("license")],["Experience",text("experience")]],drivers);
    table("trips",[["Vehicle",r=>vehicle(r.vehicleId)],["Driver",r=>driver(r.driverId)],["Origin",text("origin")],["Destination",text("destination")],["Start",r=>date(r.start)],["End",r=>date(r.end)],["Distance",text("distance")]],fleet.trips);
    table("work",[["Vehicle",r=>vehicle(r.vehicleId)],["Work",text("kind")],["Recorded",r=>date(r.recordedAt)],["Mileage",text("mileage")],["Cost EUR",r=>r.costEur??"Unknown"],["Notes",text("notes")]],work);
    table("shifts",[["Driver",r=>driver(r.driverId)],["Start",r=>date(r.start)],["End",r=>date(r.end)]],shifts);
    table("reservations",[["Vehicle",r=>vehicle(r.vehicleId)],["Category",text("kind")],["Start",r=>date(r.start)],["End",r=>date(r.end)],["Notes",text("notes")]],reservations);
    table("audit",[["Time",r=>date(r.occurredUtc)],["Actor",text("actorId")],["Entity",text("entityType")],["ID",text("entityId")],["Action",text("action")]],audit);
    for(const form of editors){form.hidden=!canWrite;for(const select of form.querySelectorAll("select[name=vehicleId]"))options(select,vehicles,v=>v.plate);for(const select of form.querySelectorAll("select[name=driverId]"))options(select,drivers,d=>d.name);}
    $("audit-tab").hidden=!admin;
    if(!admin&&currentPage==="audit")tab("fleet");
    $("updated").textContent="Last refreshed "+new Date().toLocaleString()+" · independent API reads, not a transactional snapshot.";
    $("auth").hidden=true;$("workspace").hidden=false;$("logout").hidden=false;
}
async function operation(action){if(busy)return;busy=true;const active=generation;for(const button of document.querySelectorAll("button:not(#logout)"))button.disabled=true;try{await action();}catch(e){if(active===generation)status(e.message||"Request failed.",true);}finally{if(active===generation){busy=false;for(const button of document.querySelectorAll("button"))button.disabled=false;}}}
$("login").addEventListener("submit",event=>{event.preventDefault();const data=new FormData(event.currentTarget);event.currentTarget.elements.password.value="";operation(async()=>{const result=await api("/api/auth/login","POST",{email:data.get("email"),password:data.get("password")},false);token=result.accessToken;if(typeof token!=="string"){token=null;throw Error("Missing access token.");}try{await refresh();tab("fleet");status("Connected to local API.");}catch(e){signOut("Unable to load workspace. Sign in again.");throw e;}});});
$("logout").addEventListener("click",()=>{signOut();for(const button of document.querySelectorAll("button"))button.disabled=false;});
$("refresh").addEventListener("click",()=>operation(async()=>{await refresh();status("Records refreshed.");}));
for(const button of document.querySelectorAll("[data-tab]"))button.addEventListener("click",()=>tab(button.dataset.tab));
function utc(value){return new Date(value+"Z").toISOString();}
function editor(form,path,build){form.addEventListener("submit",event=>{event.preventDefault();const data=new FormData(form);if(!data.has("confirmed"))return;operation(async()=>{await api(path,"POST",build(data));form.reset();status("Record saved. Refreshing server data…");try{await refresh();status("Record saved and server data refreshed.");}catch(e){if(token)status("Record saved, but refresh failed. Use Refresh server records; do not resubmit.",true);}});});}
editor($("work-form"),"/api/operations/maintenance",d=>({vehicleId:Number(d.get("vehicleId")),kind:d.get("kind"),notes:d.get("notes"),costEur:d.get("costEur")===""?null:Number(d.get("costEur"))}));
editor($("shift-form"),"/api/operations/shifts",d=>({driverId:Number(d.get("driverId")),start:utc(d.get("start")),end:utc(d.get("end"))}));
editor($("reservation-form"),"/api/operations/reservations",d=>({vehicleId:Number(d.get("vehicleId")),kind:d.get("kind"),notes:d.get("notes"),start:utc(d.get("start")),end:utc(d.get("end"))}));
tab("fleet");
