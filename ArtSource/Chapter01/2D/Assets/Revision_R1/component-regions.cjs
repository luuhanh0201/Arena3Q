'use strict';
// Slice interleaved leaf silhouettes by connected alpha, not a straight grid cut.
function components(im,threshold=8){
 const {w,h,data}=im,N=w*h,labels=new Int32Array(N),queue=new Int32Array(N),parts=[];let label=0;
 for(let p=0;p<N;p++){if(labels[p]||data[p*4+3]<threshold)continue;label++;let head=0,end=1,x0=w,y0=h,x1=0,y1=0;queue[0]=p;labels[p]=label;
  while(head<end){const i=queue[head++],x=i%w,y=Math.floor(i/w);x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);
   for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const xx=x+dx,yy=y+dy,j=yy*w+xx;if(xx>=0&&xx<w&&yy>=0&&yy<h&&!labels[j]&&data[j*4+3]>=threshold){labels[j]=label;queue[end++]=j;}}
  }parts.push({label,count:end,rect:{left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}});
 }return {labels,parts:parts.sort((a,b)=>b.count-a.count)};
}
function separate(im,count){
 const {labels,parts}=components(im),main=parts.slice(0,count).sort((a,b)=>a.rect.left-b.rect.left),map=new Map(main.map((p,i)=>[p.label,i+1]));
 const N=im.w*im.h,owner=new Uint8Array(N),queue=new Int32Array(N);let head=0,end=0;
 for(let i=0;i<N;i++){const v=map.get(labels[i]);if(v){owner[i]=v;queue[end++]=i;}}
 while(head<end){const i=queue[head++],x=i%im.w;for(const j of [x?i-1:-1,x<im.w-1?i+1:-1,i>=im.w?i-im.w:-1,i<N-im.w?i+im.w:-1])if(j>=0&&!owner[j]){owner[j]=owner[i];queue[end++]=j;}}
 return main.map((p,index)=>{const b=p.rect,left=Math.max(0,b.left-2),top=Math.max(0,b.top-2),right=Math.min(im.w,b.left+b.width+2),bottom=Math.min(im.h,b.top+b.height+2),w=right-left,h=bottom-top,data=Buffer.alloc(w*h*4);
  for(let y=0;y<h;y++)for(let x=0;x<w;x++){const i=(top+y)*im.w+left+x,k=(y*w+x)*4;im.data.copy(data,k,i*4,i*4+4);if(owner[i]!==index+1)data[k+3]=0;}
  return {rect:{left,top,width:w,height:h},image:{data,w,h}};
 });
}
module.exports=components;module.exports.separate=separate;
