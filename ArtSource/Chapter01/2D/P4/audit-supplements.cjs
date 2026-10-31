const fs=require('fs'),path=require('path'),sharp=require('../../../../Admin/node_modules/sharp');
(async()=>{const rows=[],layers=[];
for(const [i,name] of ['wildflowers','fallen-cloth','village-litter'].entries()){
 const file=path.join(__dirname,'Generated',name+'.png'),{data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let visible=0,opaque=0,border=0,redFringe=0;const borderAlpha={};
 for(let p=0;p<data.length;p+=4){const a=data[p+3],x=p/4%info.width,y=Math.floor(p/4/info.width);if(a>0)visible++;if(a>250)opaque++;if(x===0||y===0||x===info.width-1||y===info.height-1){borderAlpha[a]=(borderAlpha[a]||0)+1;if(a>0)border++;}if(a>20&&data[p]>180&&data[p]>data[p+1]*2&&data[p]>data[p+2]*2)redFringe++;}
 rows.push({name,width:info.width,height:info.height,visible,opaque,border,borderAlpha,redFringe});
 for(let bg=0;bg<2;bg++)layers.push({input:await sharp(file).resize(300,300).flatten({background:bg?'#292d27':'#eee9db'}).png().toBuffer(),left:i*300,top:bg*300});
}
const out=path.resolve('NghiemThu/Chapter01/XemTruoc_P4Supplement_20261031');fs.mkdirSync(out,{recursive:true});
await sharp({create:{width:900,height:600,channels:3,background:'#eee9db'}}).composite(layers).png().toFile(path.join(out,'source-alpha.png'));
fs.writeFileSync(path.join(out,'source-alpha.json'),JSON.stringify(rows,null,2));console.log(rows);
// Native imagegen leaves a few 1/255-alpha noise pixels at the outer edge.
// Keep the raw report; 14 transparent atlas gutter pixels isolate those from neighbours.
if(rows.some(r=>Object.keys(r.borderAlpha).some(a=>Number(a)>1)||r.redFringe>0))throw Error('Visible source-edge contamination');
})().catch(e=>{console.error(e);process.exitCode=1;});
