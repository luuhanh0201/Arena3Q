const $=s=>document.querySelector(s),base='/Assets/Arena3Q/Lobby2D/';
const delivery=await fetch('/ArtSource/Lobby2D/delivery.json').then(r=>r.json());
let acceptance;try{acceptance=await fetch('/ArtSource/Lobby2D/acceptance-report.json').then(r=>r.json())}catch{}
$('#state').textContent=acceptance?.webScopePassed?'Đạt kiểm tra web':'Đang nghiệm thu';
for(const button of document.querySelectorAll('nav button'))button.onclick=()=>{document.querySelector('nav .active')?.classList.remove('active');button.classList.add('active');document.querySelector('.tab.active').classList.remove('active');$('#'+button.dataset.tab).classList.add('active')};
const times=['Sáng','Trưa','Chiều','Tối','Đêm'];times.forEach((name,i)=>{const b=document.createElement('button');b.textContent=name;b.onclick=()=>{document.querySelector('#times .active')?.classList.remove('active');b.classList.add('active');$('#lobby-image').src='/NghiemThu/UI/XemTruoc_Lobby2D_20261026/Lobby_'+i+'.png';$('#lobby-caption').textContent=name+' · 1920 × 1080 · chữ và số được dựng riêng bằng TMP'};$('#times').append(b);if(!i)b.click()});
for(const b of document.querySelectorAll('#ratios button'))b.onclick=()=>{
  document.querySelector('#ratios .active')?.classList.remove('active');b.classList.add('active');
  const width=+b.dataset.width;$('.lobby-stage').style.aspectRatio=width+'/1080';
  if(width===1920)$('#times .active').click();
  else {$('#lobby-image').src='/NghiemThu/UI/XemTruoc_Lobby2D_20261026/Lobby_safe_'+width+'.png';$('#lobby-caption').textContent='Đêm · '+width+' × 1080 · safe area giả lập';}
};
function openAsset(a){$('#asset-title').textContent=a.id;$('#asset-image').src='/'+a.path;$('#asset-download').href='/'+a.path;$('#asset-dialog').showModal()}
for(const b of document.querySelectorAll('#times button')){const choose=b.onclick;b.onclick=()=>{choose();$('.lobby-stage').style.aspectRatio='16/9';document.querySelector('#ratios .active')?.classList.remove('active');$('#ratios [data-width="1920"]').classList.add('active');};}
$('#close-dialog').onclick=()=>$('#asset-dialog').close();$('#asset-dialog').onclick=e=>{if(e.target===$('#asset-dialog'))$('#asset-dialog').close()};
function fill(target,assets){$(target).replaceChildren(...assets.map(a=>{const b=document.createElement('button');b.className='asset';b.innerHTML='<div class="thumb checker"><img loading="lazy" alt=""></div><strong></strong><small></small>';b.querySelector('img').src='/'+a.path;b.querySelector('img').alt=a.id;b.querySelector('strong').textContent=a.id;b.querySelector('small').textContent=a.width+' × '+a.height+' · PNG';b.onclick=()=>openAsset(a);return b}))}
fill('#character-grid',delivery.assets.filter(a=>a.role==='portrait'));
const layerNames=['Head','Hair_Front','Hair_Back','Helmet','Feather_Left','Feather_Right','Torso','UpperArm_L','UpperArm_R','Forearm_L','Forearm_R','Hand_L','Hand_R','Thigh_L','Thigh_R','Shin_L','Shin_R','Foot_L','Foot_R','ShoulderArmor_L','ShoulderArmor_R','Cape_Upper','Cape_Left','Cape_Right','Cloth_Front','Weapon'];
fill('#layer-grid',layerNames.map(id=>({id,path:'Assets/Arena3Q/Lobby2D/Characters/LuBu/Layers/'+id+'.png',width:2048,height:2048})));
fill('#background-grid',delivery.assets.filter(a=>a.role==='sky'||a.role.startsWith('environment')));
const ui=delivery.assets.filter(a=>['icon','vip','button','frame','banner','effect'].includes(a.role));fill('#ui-grid',ui);
for(const b of document.querySelectorAll('#ui-filters button'))b.onclick=()=>{document.querySelector('#ui-filters .active')?.classList.remove('active');b.classList.add('active');fill('#ui-grid',b.dataset.role==='all'?ui:ui.filter(a=>a.role===b.dataset.role))};
let clip='Idle',index=0,playing=false,last=0,elapsed=0,loaded=0,metadata;const images=new Map();
const durations={Idle:2,Run:.9,Attack:1.1};
function url(c,i){return '/ArtSource/Lobby2D/WebFrames/'+c+'/LuBu_'+c+'_'+String(i).padStart(3,'0')+'.png'}
function frame(i){index=Math.max(0,Math.min(23,i));const path=url(clip,index);$('#frame-image').src=path;$('#download-frame').href=base+'Characters/LuBu/Animations/'+clip+'/LuBu_'+clip+'_'+String(index).padStart(3,'0')+'.png';$('#frame-slider').value=index;$('#frame-number').textContent=String(index+1).padStart(2,'0')+' / 24';document.querySelector('#frame-strip .active')?.classList.remove('active');document.querySelector('#frame-strip [data-frame="'+index+'"]')?.classList.add('active');const fx=$('#vfx-image');fx.hidden=clip!=='Attack'||!$('#vfx-toggle').checked;if(!fx.hidden)fx.src='/ArtSource/Lobby2D/WebFrames/VFX/LuBu_Attack_VFX_'+String(index).padStart(3,'0')+'.png'}
async function setClip(name){clip=name;playing=false;$('#play').textContent='Phát';elapsed=0;loaded=0;$('#clip-title').textContent=name==='Attack'?'LuBu_Attack_01':'LuBu_'+name;
  try{metadata=await fetch(base+'Characters/LuBu/Animations/'+name+'/frames.json').then(r=>{if(!r.ok)throw Error();return r.json()})}catch{metadata=null}
  $('#clip-info').textContent=(metadata?.duration||durations[name])+' giây · 24 frame · pivot cố định · '+(name==='Attack'?'đánh một lần':'lặp liên tục');
  const strip=$('#frame-strip');strip.replaceChildren();for(let i=0;i<24;i++){const b=document.createElement('button');b.dataset.frame=i;const im=new Image();im.src=url(name,i);im.alt='Frame '+(i+1);im.onload=()=>{if(clip===name){loaded++;$('#clip-status').textContent=loaded+'/24 frame đã tải'}};im.onerror=()=>{if(clip===name)$('#clip-status').textContent='Đang xuất frame, chưa hoàn tất'};b.append(im);const n=document.createElement('small');n.textContent=String(i+1).padStart(2,'0');b.append(n);b.onclick=()=>{playing=false;$('#play').textContent='Phát';frame(i)};strip.append(b);images.set(name+'_'+i,im)}frame(0)
}
for(const b of document.querySelectorAll('#clips button'))b.onclick=()=>{document.querySelector('#clips .active').classList.remove('active');b.classList.add('active');setClip(b.dataset.clip)};
$('#frame-slider').oninput=e=>{playing=false;$('#play').textContent='Phát';frame(+e.target.value)};$('#previous').onclick=()=>{playing=false;$('#play').textContent='Phát';frame((index+23)%24)};$('#next').onclick=()=>{playing=false;$('#play').textContent='Phát';frame((index+1)%24)};$('#play').onclick=()=>{if(clip==='Attack'&&index===23)frame(0);playing=!playing;elapsed=0;$('#play').textContent=playing?'Dừng':'Phát'};
$('#vfx-toggle').onchange=()=>frame(index);
$('#pivot-toggle').onchange=e=>$('.ground-pivot').hidden=!e.target.checked;$('#backdrop').onchange=e=>{$('.player').classList.remove('dark','light');if(e.target.value!=='checker')$('.player').classList.add(e.target.value)};
function tick(now){const dt=Math.min(.1,(now-last)/1000);last=now;if(playing){elapsed+=dt*+$('#speed').value;const step=(metadata?.duration||durations[clip])/24;while(elapsed>=step){elapsed-=step;if(index===23&&clip==='Attack'){playing=false;$('#play').textContent='Phát';break}frame((index+1)%24)}}requestAnimationFrame(tick)}requestAnimationFrame(tick);setClip('Idle');
window.artReview={get clip(){return clip},get frame(){return index},get playing(){return playing},get loaded(){return loaded},assetCount:delivery.assets.length};
// Browser demonstration of the same independently enabled UI effects used by the Unity prefab.
const micro=document.createElement('div');micro.className='micro-demos';
micro.innerHTML='<div class="micro-heading"><h2>Chuyển động giao diện</h2><label><input id="micro-toggle" type="checkbox" checked> Bật hiệu ứng</label></div><div class="micro-row"><button class="micro-cta"><img alt="" src="'+base+'UI/Buttons/Battle_Normal.png"><span>Xuất Chiến</span><i></i></button><div class="micro-vip"><img alt="VIP 12" src="'+base+'UI/VIP/VIP_12.png"><span>VIP 12</span><i></i></div><div class="micro-notice"><img alt="Thư" src="'+base+'UI/Icons/Mail.png"><i></i></div></div>';
$('#ui').append(micro);$('#micro-toggle').onchange=e=>micro.classList.toggle('paused',!e.target.checked);
