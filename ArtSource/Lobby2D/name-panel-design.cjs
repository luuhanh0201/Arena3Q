'use strict';
const fs=require('node:fs'),path=require('node:path');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const sharp=require('./tooling.cjs')('sharp');
const root=path.resolve(__dirname,'../..');
const evidence=path.join(root,'NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const out=path.join(evidence,'name-design_'+new Date().toISOString().replace(/[:.]/g,'-'));
const uri=file=>'data:image/png;base64,'+fs.readFileSync(path.join(root,file)).toString('base64');
const fonts=['Regular','Bold','ExtraBold'].map(weight=>path.join(root,'Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-'+weight+'.ttf'));
const vip=uri('Assets/Arena3Q/Lobby2D/UI/VIP/VIP_12.png');
const avatar=uri('Assets/Arena3Q/Art/UI/Icons/LordPortrait_lord_strategist.png');
const ribbon=uri('Assets/Arena3Q/Art/UI/Startup/UI/Sprites/Buttons/btn_red.png');
const font='font-family="Be Vietnam Pro"';
const text=(x,y,value,size=18,fill='#e6ecf5',extra='')=>`<text x="${x}" y="${y}" ${font} font-size="${size}" fill="${fill}" ${extra}>${value}</text>`;
const panel=(x,y,w,h)=>`<path d="M${x+10} ${y}H${x+w-10}L${x+w} ${y+10}V${y+h-10}L${x+w-10} ${y+h}H${x+10}L${x} ${y+h-10}V${y+10}Z" fill="#0b1422" fill-opacity=".95" stroke="#b98d42" stroke-width="1.5"/>`;
const definitions=`<defs><linearGradient id="exp" x1="0" x2="1"><stop stop-color="#f55219"/><stop offset="1" stop-color="#ffd34a"/></linearGradient><clipPath id="face"><circle cx="46" cy="68" r="31"/></clipPath><clipPath id="expClip"><rect x="152" y="105" width="122" height="6"/></clipPath></defs>`;
function ribbonSlice(x,y,w,h){
  const cap=h/52*44;
  return [[0,44,x,cap],[44,98,x+cap,w-cap*2],[142,44,x+w-cap,cap]].map(([sx,sw,dx,dw])=>`<svg x="${dx}" y="${y}" width="${dw}" height="${h}" viewBox="${sx} 0 ${sw} 52" preserveAspectRatio="none"><image href="${ribbon}" width="186" height="52"/></svg>`).join('');
}
function player(){
  return `${definitions}<image href="${vip}" x="-6" y="-8" width="382" height="134"/>
  <circle cx="46" cy="68" r="34" fill="#182033" stroke="#e2b85c" stroke-width="2.5"/>
  <image href="${avatar}" x="15" y="37" width="62" height="62" clip-path="url(#face)"/>
  <circle cx="46" cy="68" r="31.5" fill="none" stroke="#ffe3a0" stroke-opacity=".8"/>
  ${text(194,77,'Lâm Tử Yên',18,'#fff0c9','font-weight="800" text-anchor="middle" stroke="#401608" stroke-width=".6" paint-order="stroke fill"')}
  <path d="M101 95H354L359 100V118L354 123H101L96 118V100Z" fill="#27080c" stroke="#c99b4f" stroke-width="1.2"/>
  ${text(107,114,'Lv.15',13,'#f4ede2','font-weight="700"')}
  <rect x="151" y="104" width="124" height="8" fill="#0c0607" stroke="#e2b85c" stroke-width=".8"/>
  <rect x="152" y="105" width="79.3" height="6" fill="url(#exp)" clip-path="url(#expClip)"/>
  ${text(329,114,'VIP 12',12,'#ffd877','font-weight="800" text-anchor="middle"')}
  <path d="M104 123V133M351 123V133" stroke="#bb924d" stroke-width="1.5"/>
  ${ribbonSlice(96,128,263,28)}
  ${text(228,147,'Chiến Lực 1.245.680',15,'#ffe29a','font-weight="800" text-anchor="middle"')}`;
}
function badge(){
  return `<path d="M10 0H102L112 10V32L102 42H10L0 32V10Z" fill="#0b1728" stroke="#e2b85c" stroke-width="1.6"/><path d="M12 4H100L108 12V30L100 38H12L4 30V12Z" fill="none" stroke="#9e742e" stroke-width=".7"/>${text(56,27,'Chủ Tướng',17,'#ffd877','text-anchor="middle" font-weight="800"')}`;
}
function render(svg,file){
  const image=new Resvg(svg,{font:{fontFiles:fonts,loadSystemFonts:false,defaultFontFamily:'Be Vietnam Pro'}}).render().asPng();
  fs.writeFileSync(path.join(out,file),image);
}
async function main(){
  fs.mkdirSync(out,{recursive:true});
  const sourcePlayer=process.argv[2]||path.join(evidence,'name-design-sources/current-player.png');
  const sourceLord=process.argv[3]||path.join(evidence,'name-design-sources/current-lord.png');
  fs.copyFileSync(sourcePlayer,path.join(out,'current-player.png')); fs.copyFileSync(sourceLord,path.join(out,'current-lord.png'));
  const oldPlayer='data:image/png;base64,'+(await sharp(sourcePlayer).png().toBuffer()).toString('base64');
  const oldLord='data:image/png;base64,'+(await sharp(sourceLord).png().toBuffer()).toString('base64');
  const board=`<svg xmlns="http://www.w3.org/2000/svg" width="1320" height="890" viewBox="0 0 1320 890"><rect width="1320" height="890" fill="#101927"/>
  ${text(40,48,'Thiết kế đề xuất',30,'#ffd877','font-weight="800"')}${text(1280,45,'CHƯA ÁP DỤNG VÀO GAME',14,'#a9b8cf','text-anchor="end"')}
  ${text(40,98,'Ô tên hiện tại',20,'#e6ecf5','font-weight="700"')}${text(690,98,'Ô tên đề xuất',20,'#ffd877','font-weight="700"')}
  ${panel(30,115,610,310)}${panel(680,115,610,310)}
  <image href="${oldPlayer}" x="40" y="150" width="590" height="190" preserveAspectRatio="xMidYMid meet"/>
  <g transform="translate(698 148) scale(1.5)">${player()}</g>
  ${text(40,460,'Tên nằm trong ô đỏ. Lv, EXP và VIP thành hàng riêng, không chạm phần trang trí.',18,'#a9b8cf')}
  <path d="M40 490H1280" stroke="#3a485b"/>
  ${text(40,536,'Khung chủ tướng hiện tại',20,'#e6ecf5','font-weight="700"')}${text(690,536,'Khung chủ tướng đề xuất',20,'#ffd877','font-weight="700"')}
  <image href="${oldLord}" x="240" y="558" width="95" height="280" preserveAspectRatio="xMidYMid meet"/>
  <g transform="translate(873 628) scale(2)">${badge()}</g>
  ${text(985,753,'Một dòng Chủ Tướng, nhỏ gọn.',18,'#a9b8cf','text-anchor="middle"')}
  ${text(985,784,'Bỏ tên người chơi và dãy sao ở khung này.',16,'#a9b8cf','text-anchor="middle"')}
  ${text(40,866,'Bản xem bố cục dùng art và phông hiện có; chỉ áp dụng sau khi bạn duyệt.',16,'#8fa2bf')}</svg>`;
  render(board,'design-before-after.png');
  render(`<svg xmlns="http://www.w3.org/2000/svg" width="780" height="340"><rect width="780" height="340" fill="#101927"/><g transform="translate(12 16) scale(2)">${player()}</g></svg>`,'proposed-player.png');
  render(`<svg xmlns="http://www.w3.org/2000/svg" width="448" height="168"><g transform="scale(4)">${badge()}</g></svg>`,'proposed-lord.png');
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Đề xuất ô tên và khung Chủ Tướng</title><style>body{margin:0;background:#101927;color:#e6ecf5;font:17px system-ui}main{max-width:1400px;margin:auto;padding:20px}img{display:block;max-width:100%;height:auto;margin:16px auto}h1,a{color:#ffd877}p{line-height:1.6}</style><main><h1>Ảnh thiết kế trước khi áp dụng</h1><p>Tên nằm trong vùng đỏ của khung VIP. Hàng Lv/EXP/VIP tách riêng. Khung gần nhân vật chỉ giữ một dòng Chủ Tướng.</p><a href="design-before-after.png"><img src="design-before-after.png"></a><h2>Cận thiết kế đề xuất</h2><img src="proposed-player.png"><img src="proposed-lord.png"><p>Đây là bản xem bố cục, chưa sửa ô tên và nhãn Chủ Tướng trong game. Nhạc nền khi test Unity đã được tắt.</p></main></html>`);
  const relative=path.basename(out)+'/review.html';
  fs.writeFileSync(path.join(evidence,'name-design.html'),`<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=${relative}"><a href="${relative}">Xem thiết kế ô tên</a>`);
  fs.writeFileSync(path.join(out,'design.json'),JSON.stringify({appliedToGame:false,source:'existing VIP12, portrait and red-button artwork; Be Vietnam Pro',player:{name:[108,54,175,36],level:[107,101],experience:[151,104,124,8],vip:[329,114],power:[96,128,263,28]},lordBadge:{label:'Chủ Tướng',size:[112,42],showPlayerName:false,showStars:false}},null,2));
  console.log(out);
}
main().catch(error=>{console.error(error);process.exitCode=1});
