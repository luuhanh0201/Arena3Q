'use strict';
const http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const mime={'.png':'image/png','.jpg':'image/jpeg','.json':'application/json','.html':'text/html','.gif':'image/gif'};
http.createServer((request,response)=>{
  let route;try{route=decodeURIComponent(new URL(request.url,'http://localhost').pathname);}catch{response.writeHead(400).end();return;}
  const file=path.resolve(root,'.'+(route==='/'?'/review.html':route));
  if(!file.startsWith(root+path.sep)){response.writeHead(403).end();return;}
  fs.stat(file,(error,stat)=>{
    if(error||!stat.isFile()){response.writeHead(404).end();return;}
    response.writeHead(200,{'Content-Type':mime[path.extname(file)]||'text/plain','Cache-Control':'no-store'});
    fs.createReadStream(file).pipe(response);
  });
}).listen(4331,'127.0.0.1',()=>console.log('Lobby integrated review: http://127.0.0.1:4331'));
