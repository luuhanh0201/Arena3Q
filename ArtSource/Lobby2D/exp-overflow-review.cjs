'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const evidence=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const captures=fs.readdirSync(evidence).filter(name=>name.startsWith('ui_refinements_')).sort().at(-1);
const out=path.join(evidence,'exp-fix_'+new Date().toISOString().replace(/[:.]/g,'-'));
const cases=[['Experience_1_0','0%'],['Experience_1_50','50%'],['Experience_1_100','100%'],['Experience_60_0','Cấp tối đa']];
const title=(text,width)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="34"><rect width="100%" height="100%" fill="#111b29"/><text x="12" y="24" fill="#ffd877" font-size="18" font-family="Arial">${text}</text></svg>`);
async function main(){
  fs.mkdirSync(out,{recursive:true});
  for(const [name,label] of cases){
    const inputs=await Promise.all([path.join(evidence,'exp-overflow-before',name+'.png'),path.join(evidence,captures,name+'.png')]
      .map(file=>sharp(file).extract({left:0,top:0,width:600,height:310}).png().toBuffer()));
    await sharp({create:{width:1200,height:344,channels:3,background:'#111b29'}}).composite([
      {input:title('Trước / '+label,600),left:0,top:0},{input:title('Sau / '+label,600),left:600,top:0},
      {input:inputs[0],left:0,top:34},{input:inputs[1],left:600,top:34}]).png().toFile(path.join(out,name+'_before-after.png'));
  }
  const tiers=[];
  for(const tier of [1,12])for(const width of [1920,2160,2340,2400]){
    const input='Vip_'+tier+(width===1920?'':'_'+width)+'.png',file='Vip_'+tier+'_'+width+'.png';
    await sharp(path.join(evidence,captures,input)).extract({left:width===1920?0:60,top:0,width:600,height:310}).png().toFile(path.join(out,file));
    tiers.push([file,tier,width]);
  }
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><meta charset="utf-8"><title>Sửa EXP tràn khung</title><style>body{background:#101927;color:#e6ecf5;font:17px system-ui}main{max-width:1200px;margin:auto;padding:20px}img{max-width:100%;height:auto}h1,h2,a{color:#ffd877}figure{margin:20px 0}.grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}</style><main><h1>EXP nằm gọn trong hàng chỉ số</h1><p>Trước và sau cùng cấp/EXP, cùng khung 1920. Theo bố cục mới đã chọn, EXP nằm trên hàng chỉ số riêng; rãnh và phần đầy nằm chung một viewport cắt theo viền. Không đổi giá trị EXP.</p>${cases.map(([name,label])=>`<figure><img src="${name}_before-after.png"><figcaption>${label}</figcaption></figure>`).join('')}<h2>Đầy 100% ở VIP 1 và VIP 12, bốn tỉ lệ</h2><div class="grid">${tiers.map(([file,tier,width])=>`<figure><img src="${file}"><figcaption>VIP ${tier}, ${width} x 1080</figcaption></figure>`).join('')}</div><p><a href="../review.html">Toàn bộ ảnh sảnh</a></p></main>`);
  const relative=path.basename(out)+'/review.html';
  fs.writeFileSync(path.join(evidence,'exp-review.html'),`<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=${relative}"><a href="${relative}">Ảnh sửa EXP</a>`);
  console.log(out);
}
main().catch(error=>{console.error(error);process.exitCode=1});
