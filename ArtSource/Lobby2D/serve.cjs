'use strict';
const http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
const mime={'.png':'image/png','.gif':'image/gif','.jpg':'image/jpeg','.json':'application/json','.js':'text/javascript','.cjs':'text/plain','.html':'text/html','.css':'text/css','.svg':'image/svg+xml'};
const server=http.createServer((req,res)=>{
  let route;try{route=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400);res.end();return;}
  if(route==='/')route='/ArtSource/Lobby2D/web/index.html';
  const file=path.resolve(root,'.'+route);
  const allowed=['ArtSource/Lobby2D/','Assets/Arena3Q/Lobby2D/','NghiemThu/UI/XemTruoc_Lobby2D_20261026/','NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/','docs/UI/assets/'].some(prefix=>file.startsWith(path.resolve(root,prefix)+path.sep));
  if(!allowed||!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
  fs.stat(file,(error,stat)=>{
    if(error||!stat.isFile()){res.writeHead(404);res.end('Not found');return;}
    res.writeHead(200,{'Content-Type':mime[path.extname(file).toLowerCase()]||'application/octet-stream','Cache-Control':'no-store'});
    fs.createReadStream(file).pipe(res);
  });
});
server.listen(4330,'127.0.0.1',()=>console.log('Arena3Q art review: http://127.0.0.1:4330'));
