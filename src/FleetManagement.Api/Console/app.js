"use strict";
const $=id=>document.getElementById(id);
let token=null, generation=0, busy=false, currentPage="fleet";
const editors=[$("work-form"),$("shift-form"),$("reservation-form"),$("trip-form"),$("vehicle-form"),$("driver-form")];
let registerEdits={vehicles:null,drivers:null},accountsOffset=0,accountsTotal=0;
let revision=null,revisionTimer=null,checkingRevision=false;
let tripVersion=null,tripId=null,latestFleet=null,batchRequests=[],batchPlans=[],nextRequest=1;
function status(text,error=false){uiText($("status"),text);$("status").classList.toggle("error",error);}
function signOut(message="Signed out."){
    token=null;generation++;busy=false;stopRevisionWatch();
    for(const button of document.querySelectorAll("button"))button.disabled=false;
    $("auth").hidden=false;$("workspace").hidden=true;$("logout").hidden=true;
    $("identity").textContent="";uiText($("policy"),"");uiText($("updated"),"");
    for(const id of ["vehicles","drivers","trips","work","shifts","reservations","audit","finance-summary","finance-vehicles","accounts","overview-alerts","calendar-events"])$(id).replaceChildren();
    for(const id of ["vehicles-count","drivers-count","trips-count","work-count"])$(id).textContent="—";
    for(const form of editors){form.reset();form.hidden=true;for(const select of form.querySelectorAll("select[name=vehicleId],select[name=driverId]"))select.replaceChildren();}
    $("audit-tab").hidden=true;$("accounts-tab").hidden=true;$("account-form").reset();accountsOffset=0;accountsTotal=0;$("accounts-page").textContent="";clearDashboard();resetRegister("vehicles");resetRegister("drivers");$("login").reset();latestFleet=null;for(const filter of document.querySelectorAll("[data-filter]"))filter.value="";resetTrip();clearBatch();$("batch-panel").hidden=true;status(message);
}
async function api(path,method="GET",body,authenticated=true,format="json"){
    const active=generation;
    const headers={"Accept":"application/json"};
    if(authenticated){if(!token)throw Error("Sign in again.");headers.Authorization="Bearer "+token;}
    if(body!==undefined)headers["Content-Type"]="application/json";
    let response;
    try{response=await fetch(path,{method,headers,body:body===undefined?undefined:JSON.stringify(body),cache:"no-store",credentials:"omit",redirect:"error",signal:AbortSignal.timeout(30000)});}
    catch{throw Error(method==="GET"?"Server unavailable or request timed out. Refresh to retry.":"Request outcome unknown. Check server records before resubmitting.");}
    if(active!==generation)throw Error("Session changed.");
    if(response.status===401){if(authenticated)signOut("Session expired. Sign in again.");throw Error("Authentication failed.");}
    if(!response.ok){let detail;try{detail=await response.json();}catch{}throw Error(typeof detail?.error==="string"?detail.error:Array.isArray(detail?.errors)?detail.errors.filter(x=>typeof x==="string").join("; "):"Request failed (HTTP "+response.status+").");}
    const result=response.status===204?null:format==="blob"?await response.blob():await response.json();
    if(active!==generation)throw Error("Session changed.");
    return result;
}
function table(id,columns,rows){
    const root=$(id);root.replaceChildren();
    const head=document.createElement("thead"),tr=document.createElement("tr");
    for(const [label] of columns){const th=document.createElement("th");th.scope="col";uiText(th,label);tr.append(th);}
    head.append(tr);root.append(head);
    const body=document.createElement("tbody");
    for(const row of rows){const r=document.createElement("tr");for(const [,get] of columns){const td=document.createElement("td");const value=get(row);if(value instanceof Node)td.append(value);else td.textContent=value==null?"—":String(value);r.append(td);}body.append(r);}
    if(!rows.length){const r=document.createElement("tr"),td=document.createElement("td");td.colSpan=columns.length;uiText(td,"No records.");r.append(td);body.append(r);}
    root.append(body);applyFilter(id);
}
const date=x=>typeof x==="string"?x.replace("T"," ").replace(/Z$/," UTC"):"—";
const text=key=>row=>row[key];
function options(select,rows,label){const old=select.value;select.replaceChildren();const blank=document.createElement("option");blank.value="";uiText(blank,"Select");select.append(blank);for(const r of rows){const o=document.createElement("option");o.value=String(r.id);o.textContent=label(r);select.append(o);}if([...select.options].some(o=>o.value===old))select.value=old;}
function tab(name){currentPage=name;for(const item of document.querySelectorAll("[data-page]"))item.hidden=item.dataset.page!==name;for(const button of document.querySelectorAll("[data-tab]"))button.setAttribute("aria-pressed",String(button.dataset.tab===name));}
async function refresh(){
    const active=generation;
    const checkpoint=await api("/api/operations/revision");
    const [me,fleet,work,shifts,reservations,policy]=await Promise.all([api("/api/auth/me"),api("/api/fleet"),api("/api/operations/maintenance"),api("/api/operations/shifts"),api("/api/operations/reservations"),api("/api/operations/policy")]);
    if(active!==generation||!token)throw Error("Session changed.");
    const admin=me.roles.includes("Admin"),canWrite=admin||me.roles.includes("Dispatcher");
    const audit=admin?await api("/api/admin/audit?limit=100"):[];
    if(active!==generation||!token)throw Error("Session changed.");
    $("identity").textContent=me.name+" · "+me.roles.join(", ");
    uiText($("policy"),policy.requireShifts?"Strict driver shifts enabled":"Driver shifts advisory");
    latestFleet=fleet;
    const vehicles=fleet.vehicles,drivers=fleet.drivers;
    const vehicle=id=>vehicles.find(v=>v.id===id)?.plate??("#"+id);
    const driver=id=>drivers.find(d=>d.id===id)?.name??("#"+id);
    $("vehicles-count").textContent=vehicles.length;$("drivers-count").textContent=drivers.length;$("trips-count").textContent=fleet.trips.length;$("work-count").textContent=work.length;
    table("vehicles",[["Plate",text("plate")],["Model",text("model")],["Mileage (km)",text("mileage")],["Last service",r=>date(r.lastService)],...(canWrite?[["Action",v=>registerAction(v,"vehicles")]]:[])],vehicles);
    table("drivers",[["Name",text("name")],["License",text("license")],["Experience",text("experience")],...(canWrite?[["Action",d=>registerAction(d,"drivers")]]:[])],drivers);
    table("trips",[["Vehicle",r=>vehicle(r.vehicleId)],["Driver",r=>driver(r.driverId)],["Origin",text("origin")],["Destination",text("destination")],["Start",r=>date(r.start)],["End",r=>date(r.end)],["Distance",text("distance")],...(canWrite?[["Action",t=>tripAction(t)]]:[])],fleet.trips);
    table("work",[["Vehicle",r=>vehicle(r.vehicleId)],["Work",text("kind")],["Recorded",r=>date(r.recordedAt)],["Mileage",text("mileage")],["Cost EUR",r=>r.costEur??"Unknown"],["Notes",text("notes")]],work);
    table("shifts",[["Driver",r=>driver(r.driverId)],["Start",r=>date(r.start)],["End",r=>date(r.end)],...(canWrite?[["Action",r=>cancelAction(r,"shifts")]]:[])],shifts);
    table("reservations",[["Vehicle",r=>vehicle(r.vehicleId)],["Category",text("kind")],["Start",r=>date(r.start)],["End",r=>date(r.end)],["Notes",text("notes")],...(canWrite?[["Action",r=>cancelAction(r,"reservations")]]:[])],reservations);
    table("audit",[["Time",r=>date(r.occurredUtc)],["Actor",text("actorId")],["Entity",text("entityType")],["ID",text("entityId")],["Action",text("action")]],audit);
    for(const form of editors){form.hidden=!canWrite;for(const select of form.querySelectorAll("select[name=vehicleId]"))options(select,vehicles,v=>v.plate);for(const select of form.querySelectorAll("select[name=driverId]"))options(select,drivers,d=>d.name);}
    $("batch-panel").hidden=!canWrite;
    $("audit-tab").hidden=!admin;$("accounts-tab").hidden=!admin;
    if(admin&&currentPage==="accounts")await loadAccounts();
    if(currentPage==="overview")await loadDashboard();
    if(currentPage==="calendar")await loadCalendar();
    if(!admin&&["audit","accounts"].includes(currentPage))tab("fleet");
    uiText($("updated"),"Last refreshed "+new Date().toLocaleString()+" · independent API reads, not a transactional snapshot.");
    revision=checkpoint.revision;$("change-notice").hidden=true;
    $("auth").hidden=true;$("workspace").hidden=false;$("logout").hidden=false;
    startRevisionWatch();
}
async function operation(action){if(busy)return;busy=true;const active=generation;for(const button of document.querySelectorAll("button:not(#logout)"))button.disabled=true;try{await action();}catch(e){if(active===generation)status(e.message||"Request failed.",true);}finally{if(active===generation){busy=false;for(const button of document.querySelectorAll("button"))button.disabled=false;}}}
$("login").addEventListener("submit",event=>{event.preventDefault();const data=new FormData(event.currentTarget);event.currentTarget.elements.password.value="";operation(async()=>{const result=await api("/api/auth/login","POST",{email:data.get("email"),password:data.get("password")},false);token=result.accessToken;if(typeof token!=="string"){token=null;throw Error("Missing access token.");}try{await refresh();tab("fleet");status("Connected to local API.");}catch(e){if(token)signOut("Unable to load workspace. Sign in again.");throw e;}});});
$("logout").addEventListener("click",()=>{signOut();for(const button of document.querySelectorAll("button"))button.disabled=false;});
$("refresh").addEventListener("click",()=>operation(async()=>{await refresh();status("Records refreshed.");}));
for(const button of document.querySelectorAll("[data-tab]"))button.addEventListener("click",()=>tab(button.dataset.tab));
function utc(value){return new Date(value+"Z").toISOString();}
function editor(form,path,build){form.addEventListener("submit",event=>{event.preventDefault();const data=new FormData(form);if(!data.has("confirmed"))return;operation(async()=>{const active=generation;await api(path,"POST",build(data));form.reset();status("Record saved. Refreshing server data…");try{await refresh();status("Record saved and server data refreshed.");}catch(e){if(token&&active===generation)status("Record saved, but refresh failed. Use Refresh server records; do not resubmit.",true);}});});}
editor($("work-form"),"/api/operations/maintenance",d=>({vehicleId:Number(d.get("vehicleId")),kind:d.get("kind"),notes:d.get("notes"),costEur:d.get("costEur")===""?null:Number(d.get("costEur"))}));
editor($("shift-form"),"/api/operations/shifts",d=>({driverId:Number(d.get("driverId")),start:utc(d.get("start")),end:utc(d.get("end"))}));
editor($("reservation-form"),"/api/operations/reservations",d=>({vehicleId:Number(d.get("vehicleId")),kind:d.get("kind"),notes:d.get("notes"),start:utc(d.get("start")),end:utc(d.get("end"))}));
tab("fleet");

const financeForm=$("finance-form");
const today=new Date().toISOString().slice(0,10);
financeForm.elements.from.value=new Date(Date.now()-29*86400000).toISOString().slice(0,10);
financeForm.elements.to.value=today;
function financeInput(){
    const d=new FormData(financeForm);
    const n=key=>Number(d.get(key));
    return {from:d.get("from"),to:d.get("to"),rates:{litresPer100Km:n("litresPer100Km"),fuelPrice:n("fuelPrice"),maintenancePerKm:n("maintenancePerKm"),driverPerHour:n("driverPerHour")},budget:n("budget"),fixedAllowance:n("fixedAllowance")};
}
const eur=value=>value==null?"—":new Intl.NumberFormat("en",{style:"currency",currency:"EUR"}).format(value);
financeForm.addEventListener("submit",event=>{event.preventDefault();operation(async()=>{
    const data=await api("/api/reports/finance/summary","POST",financeInput());
    const actual=data.completedEstimate,plan=data.budgetProjection,journal=data.recordedMaintenance;
    table("finance-summary",[["Metric",r=>r[0]],["Value",r=>r[1]]],[
        ["Completed distance (km)",actual.distanceKm],["Completed-trip estimate",eur(actual.total)],["Cost per km",eur(actual.costPerKm)],
        ["Recorded maintenance (separate)",eur(journal.recordedCost)],["Unknown work costs",journal.entries-journal.withCost],
        ["Projected whole-period total",eur(plan.projectedTotal)],["Period budget",eur(plan.budget)],["Budget headroom",eur(plan.headroom)],["Calculated at",date(data.asOfUtc)]]);
    table("finance-vehicles",[["Vehicle",text("plate")],["Distance (km)",text("distanceKm")],["Estimated cost",r=>eur(r.total)],["EUR/km",r=>eur(r.costPerKm)]],actual.vehicles);
    status("Financial report calculated from server records.");
});});
financeForm.addEventListener("input",()=>{for(const id of ["finance-summary","finance-vehicles"])$(id).replaceChildren();});
$("finance-export").addEventListener("click",()=>{if(!financeForm.reportValidity())return;operation(async()=>{
    const input=financeInput();
    const blob=await api("/api/reports/finance/excel","POST",input,true,"blob");
    const url=URL.createObjectURL(blob);const link=document.createElement("a");
    link.href=url;link.download="fleet-finance-"+input.from+"-"+input.to+".xlsx";
    document.body.append(link);link.click();link.remove();setTimeout(()=>URL.revokeObjectURL(url),1000);
    status("Financial workbook downloaded.");
});});

const tripForm=$("trip-form"),batchForm=$("batch-form");
function tripFields(d){return {vehicleId:Number(d.get("vehicleId")),driverId:Number(d.get("driverId")),origin:d.get("origin").trim(),destination:d.get("destination").trim(),start:utc(d.get("start")),end:utc(d.get("end")),distance:Number(d.get("distance"))};}
function versionOf(t){return {vehicleId:t.vehicleId,driverId:t.driverId,origin:t.origin,destination:t.destination,start:t.start,end:t.end,distance:t.distance};}
function resetTrip(){tripId=null;tripVersion=null;$("trip-form").reset();uiText($("trip-heading"),"Create trip");uiText($("trip-edit-status"),"");}
function tripAction(t){if(new Date(t.start)<=new Date()){const span=document.createElement("span");uiText(span,"History");return span;}const button=document.createElement("button");button.type="button";uiText(button,"Edit trip");button.addEventListener("click",()=>{if(busy)return;tripId=t.id;tripVersion=versionOf(t);const e=tripForm.elements;for(const k of ["vehicleId","driverId","origin","destination","distance"])e[k].value=t[k];e.start.value=new Date(t.start).toISOString().slice(0,19);e.end.value=new Date(t.end).toISOString().slice(0,19);e.confirmed.checked=false;uiText($("trip-heading"),"Edit trip #"+t.id);uiText($("trip-edit-status"),"Review current values before saving. A changed original record is rejected.");tripForm.scrollIntoView({block:"nearest"});});return button;}
$("trip-new").addEventListener("click",resetTrip);
tripForm.addEventListener("submit",event=>{event.preventDefault();const d=new FormData(tripForm);if(!d.has("confirmed"))return;operation(async()=>{const body=tripFields(d);if(tripVersion)body.expected=tripVersion;await api(tripId===null?"/api/trips":"/api/trips/"+tripId,tripId===null?"POST":"PUT",body);resetTrip();status("Trip saved. Refreshing…");try{await refresh();status("Trip saved and server data refreshed.");}catch{if(token)status("Trip saved; refresh failed. Do not resubmit.",true);}});});
function renderRequests(){table("batch-requests",[["Request",text("id")],["Origin",text("origin")],["Destination",text("destination")],["Start",r=>date(r.start)],["End",r=>date(r.end)],["Distance",text("distance")]],batchRequests);}
function invalidateBatch(){batchPlans=[];$("batch-result").hidden=true;$("batch-confirm").checked=false;$("batch-assigned").replaceChildren();$("batch-unassigned").replaceChildren();$("batch-strategy").replaceChildren();uiText($("batch-counts"),"");}
function clearBatch(){batchRequests=[];nextRequest=1;batchForm.reset();invalidateBatch();renderRequests();}
batchForm.addEventListener("submit",event=>{event.preventDefault();if(busy)return;if(batchRequests.length>=40){status("At most 40 requests.",true);return;}const d=new FormData(batchForm);const start=utc(d.get("start")),end=utc(d.get("end"));if(new Date(end)<=new Date(start)||new Date(start)<=new Date()){status("Choose a future positive interval.",true);return;}batchRequests.push({id:nextRequest++,origin:d.get("origin").trim(),destination:d.get("destination").trim(),start,end,distance:Number(d.get("distance"))});invalidateBatch();renderRequests();status("Request added.");});
$("batch-clear").addEventListener("click",()=>{clearBatch();status("Pending requests cleared.");});
$("batch-preview").addEventListener("click",()=>operation(async()=>{if(!batchRequests.length)throw Error("Add at least one request.");invalidateBatch();batchPlans=await api("/api/operations/batch/preview","POST",{requests:batchRequests});const select=$("batch-strategy");for(const [i,p] of batchPlans.entries()){const o=document.createElement("option");o.value=i;o.textContent=p.strategy+" ("+p.assigned.length+" assigned)";select.append(o);}select.value=String(batchPlans.length-1);renderPlan();status("Preview ready; server register unchanged.");}));
function renderPlan(){const p=batchPlans[Number($("batch-strategy").value)];if(!p)return;$("batch-confirm").checked=false;$("batch-result").hidden=false;uiText($("batch-counts"),p.assigned.length+" assigned · "+p.unassigned.length+" pending");table("batch-assigned",[["Request",r=>r.request.id],["Route",r=>r.request.origin+" → "+r.request.destination],["Vehicle",r=>latestFleet?.vehicles.find(v=>v.id===r.vehicleId)?.plate??r.vehicleId],["Driver",r=>latestFleet?.drivers.find(v=>v.id===r.driverId)?.name??r.driverId]],p.assigned);table("batch-unassigned",[["Pending request",text("id")],["Origin",text("origin")],["Destination",text("destination")]],p.unassigned);}
$("batch-strategy").addEventListener("change",renderPlan);
$("batch-apply").addEventListener("click",()=>{if(!$("batch-confirm").checked){status("Confirm the selected assignments first.",true);return;}operation(async()=>{const p=batchPlans[Number($("batch-strategy").value)];if(!p?.assigned.length)throw Error("No assigned requests.");const saved=await api("/api/operations/batch/apply","POST",{confirmed:true,assignments:p.assigned});const ids=new Set(p.assigned.map(a=>a.request.id));batchRequests=batchRequests.filter(r=>!ids.has(r.id));invalidateBatch();renderRequests();status("Applied "+saved.length+" trips. Refreshing…");try{await refresh();status("Applied "+saved.length+" trips and refreshed server data.");}catch{if(token)status("Batch saved; refresh failed. Do not resubmit.",true);}});});
renderRequests();

function stopRevisionWatch(){if(revisionTimer!==null)clearInterval(revisionTimer);revisionTimer=null;revision=null;checkingRevision=false;$("change-notice").hidden=true;uiText($("connection-state"),"");}
function startRevisionWatch(){if(revisionTimer!==null)return;uiText($("connection-state"),"Change monitoring: every 10 seconds while this page is open.");revisionTimer=setInterval(checkRevision,10000);}
async function checkRevision(){
    if(!token||busy||checkingRevision||document.hidden)return;
    const active=generation;checkingRevision=true;
    try{
        const result=await api("/api/operations/revision");
        if(active!==generation||!token)return;
        if(result.revision!==revision){$("change-notice").hidden=false;uiText($("change-message"),"Server records changed. Refresh to review them; your current forms will not be overwritten automatically.");}
        uiText($("connection-state"),"Change monitoring active · "+new Date().toLocaleTimeString());
    }catch(e){if(active===generation&&token)uiText($("connection-state"),"Change monitoring temporarily unavailable. Use manual refresh.");}
    finally{if(active===generation)checkingRevision=false;}
}
$("change-refresh").addEventListener("click",()=>operation(async()=>{await refresh();status("Records refreshed.");}));
document.addEventListener("visibilitychange",()=>{if(!document.hidden)checkRevision();});

function applyFilter(id){const filter=document.querySelector('[data-filter="'+id+'"]');if(!filter)return;const q=filter.value.trim().toLocaleLowerCase();for(const row of $(id).querySelectorAll("tbody tr"))row.hidden=q!==""&&!row.textContent.toLocaleLowerCase().includes(q);}
for(const filter of document.querySelectorAll("[data-filter]"))filter.addEventListener("input",()=>applyFilter(filter.dataset.filter));
function cancelAction(record,kind){
    if(new Date(record.start)<=new Date()){const span=document.createElement("span");uiText(span,"History");return span;}
    const button=document.createElement("button");button.type="button";uiText(button,kind==="shifts"?"Cancel shift":"Cancel reservation");button.className="danger-button";
    button.addEventListener("click",()=>{if(busy||!window.confirm(translate("Cancel this future record?")))return;
        operation(async()=>{await api("/api/operations/"+kind+"/"+record.id,"DELETE");status("Record cancelled. Refreshing…");try{await refresh();status("Record cancelled and server data refreshed.");}catch{if(token)status("Record cancelled; refresh failed. Do not resubmit.",true);}});
    });return button;
}

const registerConfig={
    vehicles:{form:$("vehicle-form"),heading:$("vehicle-heading"),create:"Register vehicle",edit:"Edit vehicle",fields:["plate","model","mileage","lastServiceMileage","lastService","serviceIntervalKm","serviceIntervalDays"],build:d=>({plate:d.get("plate"),model:d.get("model"),mileage:Number(d.get("mileage")),lastServiceMileage:Number(d.get("lastServiceMileage")),lastService:d.get("lastService")+"T00:00:00Z",serviceIntervalKm:Number(d.get("serviceIntervalKm")),serviceIntervalDays:Number(d.get("serviceIntervalDays"))})},
    drivers:{form:$("driver-form"),heading:$("driver-heading"),create:"Register driver",edit:"Edit driver",fields:["name","license","experience"],build:d=>({name:d.get("name"),license:d.get("license"),experience:Number(d.get("experience"))})}
};
function resetRegister(kind){const c=registerConfig[kind];registerEdits[kind]=null;c.form.reset();uiText(c.heading,c.create);}
function registerAction(row,kind){const c=registerConfig[kind],button=document.createElement("button");button.type="button";uiText(button,c.edit);button.addEventListener("click",()=>{if(busy)return;
    registerEdits[kind]={id:row.id,expected:Object.fromEntries(c.fields.map(k=>[k,row[k]]))};
    for(const k of c.fields)c.form.elements[k].value=k==="lastService"?row[k].slice(0,10):row[k];c.form.elements.confirmed.checked=false;uiText(c.heading,c.edit);c.form.scrollIntoView({block:"nearest"});
});return button;}
for(const [kind,c] of Object.entries(registerConfig)){
    $(kind==="vehicles"?"vehicle-new":"driver-new").addEventListener("click",()=>resetRegister(kind));
    c.form.addEventListener("submit",event=>{event.preventDefault();const d=new FormData(c.form);if(!d.has("confirmed"))return;operation(async()=>{
        const edit=registerEdits[kind],body=c.build(d);if(edit)body.expected=edit.expected;
        await api("/api/"+kind+(edit?"/"+edit.id:""),edit?"PUT":"POST",body);resetRegister(kind);status("Record saved. Refreshing server data…");try{await refresh();status("Record saved and server data refreshed.");}catch{if(token)status("Record saved, but refresh failed. Use Refresh server records; do not resubmit.",true);}
    });});
}
async function loadAccounts(){
    const result=await api("/api/admin/users?offset="+accountsOffset+"&limit=50");
    accountsTotal=result.total;accountsOffset=result.offset;
    table("accounts",[["Email",text("email")],["Roles",r=>r.roles.join(", ")],["Locked",r=>r.locked?"Yes":"No"]],result.accounts);
    $("accounts-page").textContent=(accountsTotal===0?0:accountsOffset+1)+"–"+Math.min(accountsOffset+50,accountsTotal)+" / "+accountsTotal;
}
$("accounts-tab").addEventListener("click",()=>operation(loadAccounts));
$("accounts-prev").addEventListener("click",()=>operation(async()=>{accountsOffset=Math.max(0,accountsOffset-50);await loadAccounts();}));
$("accounts-next").addEventListener("click",()=>operation(async()=>{if(accountsOffset+50<accountsTotal){accountsOffset+=50;await loadAccounts();}}));
$("account-form").addEventListener("submit",event=>{event.preventDefault();const form=event.currentTarget,d=new FormData(form);if(!d.has("confirmed")||busy)return;form.elements.password.value="";operation(async()=>{
    await api("/api/admin/users","POST",{email:d.get("email"),password:d.get("password"),role:d.get("role")});form.reset();status("Account created.");try{await loadAccounts();}catch{if(token)status("Account created; refresh failed. Do not resubmit.",true);}
});});

let calendarEntries=[];
const calendarForm=$("calendar-form");
calendarForm.elements.from.value=new Date().toISOString().slice(0,10);
function clearDashboard(){calendarEntries=[];for(const id of ["trip-bars","service-bars","departure-bars","reserved-now","overview-time"])$(id).replaceChildren();$("calendar-filter").value="";calendarForm.reset();calendarForm.elements.from.value=new Date().toISOString().slice(0,10);}
function barRows(id,items){
    const root=$(id);root.replaceChildren();const max=Math.max(1,...items.map(x=>x[1]));
    for(const [label,value] of items){const row=document.createElement("div");row.className="dashboard-bar";
        const name=document.createElement("span"),amount=document.createElement("strong"),bar=document.createElement("progress");
        uiText(name,label);amount.textContent=String(value);bar.max=max;bar.value=value;bar.setAttribute("aria-label",translate(label));row.append(name,amount,bar);root.append(row);}
}
async function loadDashboard(){
    const data=await api("/api/operations/overview");
    barRows("trip-bars",[["Completed",data.trips.completed],["In transit",data.trips.active],["Scheduled",data.trips.scheduled]]);
    barRows("service-bars",[["Healthy",data.service.healthy],["Soon",data.service.soon],["Due",data.service.due]]);
    barRows("departure-bars",data.departures.map(d=>[d.date,d.count]));
    uiText($("reserved-now"),"Reserved now: "+data.reservedNow);
    uiText($("overview-time"),"Calculated at "+date(data.asOfUtc));
    table("overview-alerts",[["Severity",text("severity")],["Subject",text("subject")],["Alert",text("title")],["Evidence",text("evidence")],["Recommendation",text("recommendation")]],data.alerts);
}
async function loadCalendar(){
    if(!calendarForm.reportValidity())return;
    const d=new FormData(calendarForm);const result=await api("/api/operations/calendar?"+new URLSearchParams({from:d.get("from"),days:d.get("days")}));
    calendarEntries=result.entries;renderCalendar();
}
function renderCalendar(){const kind=$("calendar-filter").value;table("calendar-events",[["Category",text("kind")],["Subject",text("title")],["Detail",text("detail")],["Start",r=>date(r.start)],["End",r=>date(r.end)]],calendarEntries.filter(e=>!kind||e.kind===kind));}
document.querySelector('[data-tab="overview"]').addEventListener("click",()=>operation(loadDashboard));
document.querySelector('[data-tab="calendar"]').addEventListener("click",()=>operation(loadCalendar));
calendarForm.addEventListener("submit",e=>{e.preventDefault();operation(loadCalendar);});
$("calendar-filter").addEventListener("change",renderCalendar);
