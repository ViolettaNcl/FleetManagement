// Procedural WebGL illustration, not a live fleet view. No remote assets or dependencies.
export function createScene(canvas, stage) {
  if (!canvas.isConnected || !stage.isConnected) return null;
  let gl;
  try { gl=canvas.getContext('webgl',{alpha:true,antialias:true,powerPreference:'low-power',preserveDrawingBuffer:false}); } catch { return null; }
  if(!gl)return null;
  const vs='attribute vec3 aPosition;attribute vec3 aColor;uniform float uYaw;uniform float uAspect;varying vec3 vColor;void main(){float c=cos(uYaw),s=sin(uYaw);vec3 p=vec3(c*aPosition.x-s*aPosition.z,aPosition.y,s*aPosition.x+c*aPosition.z);float y=.78*p.y-.625*p.z;float z=.625*p.y+.78*p.z;float w=18.0-z;gl_Position=vec4(p.x*2.15/uAspect,y*2.15,(w-18.0)*.08,w);vColor=aColor;}';
  const fs='precision mediump float;varying vec3 vColor;void main(){gl_FragColor=vec4(vColor,1.0);}';
  function compile(type,source){const shader=gl.createShader(type);gl.shaderSource(shader,source);gl.compileShader(shader);if(!gl.getShaderParameter(shader,gl.COMPILE_STATUS)){gl.deleteShader(shader);return null;}return shader;}
  const vertex=compile(gl.VERTEX_SHADER,vs),fragment=compile(gl.FRAGMENT_SHADER,fs);
  if(!vertex||!fragment){if(vertex)gl.deleteShader(vertex);if(fragment)gl.deleteShader(fragment);return null;}
  const program=gl.createProgram();gl.attachShader(program,vertex);gl.attachShader(program,fragment);gl.linkProgram(program);
  gl.deleteShader(vertex);gl.deleteShader(fragment);
  if(!gl.getProgramParameter(program,gl.LINK_STATUS)){gl.deleteProgram(program);return null;}
  const buffer=gl.createBuffer(),pos=gl.getAttribLocation(program,'aPosition'),color=gl.getAttribLocation(program,'aColor'),yawUniform=gl.getUniformLocation(program,'uYaw'),aspect=gl.getUniformLocation(program,'uAspect');
  const motion=matchMedia('(prefers-reduced-motion: reduce)');
  let playing=!motion.matches,visible=true,disposed=false,lost=false,frame=0,last=0,time=0,yaw=.60;
  const fixed=[];
  const palette={base:[.13,.095,.24],grid:[.27,.22,.43],road:[.31,.22,.62],violet:[.56,.30,.98],cyan:[.18,.82,.95],pink:[.85,.32,.72],body:[.69,.58,.98],cab:[.16,.56,.84],wheel:[.10,.09,.17],glass:[.35,.8,.93]};
  function quad(out,a,b,c,d,col,shade=1){for(const p of [a,b,c,a,c,d])out.push(...p,...col.map(v=>v*shade));}
  function box(out,x,y,z,w,h,d,col){
    const l=x-w/2,r=x+w/2,b=y,t=y+h,n=z-d/2,f=z+d/2;
    const a=[l,b,n],q=[r,b,n],c=[r,b,f],e=[l,b,f],A=[l,t,n],B=[r,t,n],C=[r,t,f],D=[l,t,f];
    quad(out,A,B,C,D,col,1);quad(out,a,q,B,A,col,.72);quad(out,q,c,C,B,col,.58);quad(out,c,e,D,C,col,.83);quad(out,e,a,A,D,col,.65);
  }
  function line(out,x1,z1,x2,z2,y,width,col){
    const dx=x2-x1,dz=z2-z1,length=Math.hypot(dx,dz);if(!length)return;
    const u=-dz/length*width/2,v=dx/length*width/2;
    quad(out,[x1+u,y,z1+v],[x2+u,y,z2+v],[x2-u,y,z2-v],[x1-u,y,z1-v],col);
  }
  box(fixed,0,-.48,0,12,.42,9,palette.base);
  for(let x=-5;x<=5;x++)line(fixed,x,-4,x,4,0,.024,palette.grid);
  for(let z=-4;z<=4;z++)line(fixed,-5,z,5,z,0,.024,palette.grid);
  const route=[[-4,-2],[-1,-2],[-1,1.8],[3.8,1.8],[3.8,-2],[-4,-2]];
  for(let i=0;i<route.length-1;i++){const [x,z]=route[i],[u,v]=route[i+1];line(fixed,x,z,u,v,.035,.18,palette.road);line(fixed,x,z,u,v,.06,.045,palette.cyan);}
  for(const [x,z] of route.slice(0,-1)){box(fixed,x,.07,z,.4,.18,.4,palette.violet);box(fixed,x,.25,z,.15,.37,.15,palette.cyan);}
  for(const [x,z,w,h,d] of [[-3.4,2.7,1.3,.75,1.1],[.6,-3.1,1.4,.65,.8],[2.3,-.5,1.2,.85,.8]]){box(fixed,x,0,z,w,h,d,palette.road);box(fixed,x,h,z,w,.05,d,palette.violet);}
  function truck(out,x,z){
    box(out,x,.13,z,1.65,.17,.65,palette.wheel);box(out,x-.28,.30,z,1.05,.65,.64,palette.body);
    box(out,x+.62,.30,z,.62,.5,.62,palette.cab);box(out,x+.76,.68,z,.31,.12,.57,palette.glass);
    for(const a of [-.57,.58])for(const b of [-.33,.33])box(out,x+a,.02,z+b,.27,.26,.18,palette.wheel);
    box(out,x+.97,.36,z,.045,.12,.45,palette.cyan);
  }
  function render(){
    if(disposed||lost||!canvas.isConnected)return;
    const rect=stage.getBoundingClientRect(),dpr=Math.min(devicePixelRatio||1,1.5);
    const w=Math.max(1,Math.round(rect.width*dpr)),h=Math.max(1,Math.round(rect.height*dpr));
    if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}
    const vertices=fixed.slice();
    // The animated truck travels a bounded illustrative straight road. Other trucks are static.
    truck(vertices,-3.5+Math.abs((time*.7)%14-7),-2);
    truck(vertices,1.1,1.8);
    gl.viewport(0,0,w,h);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
    gl.enable(gl.DEPTH_TEST);gl.useProgram(program);gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(vertices),gl.DYNAMIC_DRAW);
    gl.enableVertexAttribArray(pos);gl.vertexAttribPointer(pos,3,gl.FLOAT,false,24,0);gl.enableVertexAttribArray(color);gl.vertexAttribPointer(color,3,gl.FLOAT,false,24,12);
    gl.uniform1f(yawUniform,yaw);gl.uniform1f(aspect,w/h);gl.drawArrays(gl.TRIANGLES,0,vertices.length/6);
  }
  function stop(){if(frame)cancelAnimationFrame(frame);frame=0;last=0;}
  function tick(now){frame=0;if(disposed||lost||!playing||!visible||document.hidden||!canvas.isConnected){last=0;return;}if(!last)last=now;if(now-last>=32){time+=Math.min(.1,(now-last)/1000);last=now;render();}frame=requestAnimationFrame(tick);}
  function sync(){stop();if(disposed||lost||!canvas.isConnected)return;if(visible&&!document.hidden){render();if(playing)frame=requestAnimationFrame(tick);}}
  function motionChanged(){if(motion.matches)playing=false;sync();}
  function contextLost(event){event.preventDefault();lost=true;stop();delete stage.dataset.renderer;}
  const resize=new ResizeObserver(()=>{if(visible&&!document.hidden)render();});resize.observe(stage);
  const observer=new IntersectionObserver(entries=>{visible=entries.some(e=>e.isIntersecting);sync();});observer.observe(stage);
  document.addEventListener('visibilitychange',sync);motion.addEventListener('change',motionChanged);canvas.addEventListener('webglcontextlost',contextLost);
  stage.dataset.renderer='webgl';sync();
  return {isPlaying:()=>playing&&!lost,rotate(direction){if(lost||disposed)return;yaw+=Math.sign(direction)*.22;render();},toggle(){if(lost||disposed)return false;playing=!playing;sync();return playing;},dispose(){if(disposed)return;disposed=true;stop();resize.disconnect();observer.disconnect();document.removeEventListener('visibilitychange',sync);motion.removeEventListener('change',motionChanged);canvas.removeEventListener('webglcontextlost',contextLost);delete stage.dataset.renderer;if(!lost){gl.deleteBuffer(buffer);gl.deleteProgram(program);}}};
}
