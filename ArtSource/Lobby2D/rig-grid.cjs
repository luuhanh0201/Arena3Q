const sharp=require('./tooling.cjs')('sharp');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
let svg='<svg xmlns="http://www.w3.org/2000/svg" width="1254" height="1254">';
for(let i=100;i<1254;i+=100)svg+=`<path d="M${i} 0V1254 M0 ${i}H1254" stroke="cyan" stroke-width="1" opacity=".5"/><text x="${i}" y="20" fill="white" font-size="20">${i}</text><text x="0" y="${i}" fill="white" font-size="20">${i}</text>`;
svg+='</svg>';
sharp('Assets/Arena3Q/Lobby2D/Characters/LuBu/Portraits/LuBu_FullBody.png').flatten({background:'#17243a'})
  .composite([{input:new Resvg(svg).render().asPng()}]).png().toFile('NghiemThu/UI/XemTruoc_Lobby2D_20261026/RigGrid.png');
