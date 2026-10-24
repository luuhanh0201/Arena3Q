// Render the provided DC HTML without changing it. Small DC template adapter used only for visual QA.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http');
const root = path.resolve(__dirname, '../../..');
const {chromium} = require(path.join(root,'AgentComms/codex/staging/ui-tools/node_modules/playwright'));
const out = path.join(root,'NghiemThu/UI/XemTruoc_Startup_20261024/reference'); fs.mkdirSync(out,{recursive:true});
const server = http.createServer((req,res)=>{
  const pathname = decodeURIComponent(new URL(req.url,'http://localhost').pathname);
  const file = path.resolve(root,'.'+pathname);
  if (!file.startsWith(root+path.sep) || !fs.existsSync(file) || !fs.statSync(file).isFile()) {res.writeHead(404);res.end();return;}
  const ext = path.extname(file); const types = {'.html':'text/html; charset=utf-8','.css':'text/css','.js':'text/javascript','.ttf':'font/ttf','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg'};
  res.setHeader('Content-Type',types[ext]||'application/octet-stream');fs.createReadStream(file).pipe(res);
});
async function main(){
 await new Promise(resolve=>server.listen(9232,'127.0.0.1',resolve));
 const browser = await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 try {
  const page = await browser.newPage({viewport:{width:1280,height:720},deviceScaleFactor:1});
  const pages = ['Main','Loading','Login','Register','Start','ServerSelect','BindAccount','LoadingWeak','LoadingStall','LoadingLost','LoadingTimeout'];
  for(const name of pages){
   await page.goto(`http://127.0.0.1:9232/docs/UI/Arena3Q_UI_Handoff/reference/mockup/${name}.dc.html`,{waitUntil:'domcontentloaded'});
   await page.evaluate(()=>{
     const style=document.createElement('style');style.textContent=`@font-face{font-family:'Be Vietnam Pro';src:url('/Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-Regular.ttf');font-weight:400}@font-face{font-family:'Be Vietnam Pro';src:url('/Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-SemiBold.ttf');font-weight:600}@font-face{font-family:'Be Vietnam Pro';src:url('/Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-Bold.ttf');font-weight:700}@font-face{font-family:'Be Vietnam Pro';src:url('/Assets/Arena3Q/Art/UI/Startup/Fonts/BeVietnamPro-ExtraBold.ttf');font-weight:800}*{animation-play-state:paused!important;transition:none!important}x-dc,helmet{display:block}helmet{height:0}body{margin:0}`;document.head.append(style);
     globalThis.DCLogic=class{constructor(p){this.props=p;this.state={}}setState(s){this.state={...this.state,...s}}};
     const script=document.querySelector('script[type="text/x-dc"]');
     if(script){
       const props={scenario:'auto',signal:3};
       const App=Function(script.textContent+';return Component;')();const app=new App(props);
       const vals=app.renderVals?app.renderVals():{};
       function value(raw,scope){const m=/^\s*\{\{([\s\S]*?)\}\}\s*$/.exec(raw||'');if(!m)return raw;try{return Function('s','with(s){return ('+m[1]+')}')(scope)}catch{return ''}}
       function replace(str,scope){return str.replace(/\{\{([\s\S]*?)\}\}/g,(_,e)=>{try{return String(Function('s','with(s){return ('+e+')}')(scope)??'')}catch{return ''}})}
       function walk(node,scope){
         if(node.nodeType===3){node.textContent=replace(node.textContent,scope);return;}
         if(node.nodeType!==1)return;
         if(node.tagName==='SCRIPT')return;
         if(node.tagName==='SC-FOR'){
           const list=value(node.getAttribute('list'),scope);if(Array.isArray(list)){for(const item of list){for(const child of [...node.childNodes]){const clone=child.cloneNode(true);node.before(clone);walk(clone,{...scope,[node.getAttribute('as')]:item})}}}node.remove();return;
         }
         if(node.tagName==='SC-IF'){let show=value(node.getAttribute('value'),scope);if(show===''||show===undefined)show=value(node.getAttribute('hint-placeholder-val'),scope);if(!show){node.remove();return;}}
         for(const attr of [...node.attributes]){if(!attr.name.startsWith('on'))node.setAttribute(attr.name,replace(attr.value,scope));}
         for(const child of [...node.childNodes])walk(child,scope);
       }
       walk(document.querySelector('x-dc'),vals);
     }
     for(const imp of document.querySelectorAll('dc-import'))imp.innerHTML='<div style="height:26px;width:120px;border-radius:13px;background:#0008;color:#fff;display:flex;align-items:center;justify-content:center;font-size:13px">Wi-Fi | 16:47</div>';
   });
   await page.evaluate(()=>document.fonts.ready);
   await page.screenshot({path:path.join(out,name+'.png')});
   console.log(name);
  }
 }finally{await browser.close();server.close();}
}
main().catch(e=>{console.error(e);server.close();process.exitCode=1});
