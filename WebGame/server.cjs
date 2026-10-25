// Serve the packaged game only. No repository, credentials, Admin APIs or source assets are exposed.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const settings = require('./web-settings.json');
const root = path.resolve(__dirname, '..', settings.output);
const host = process.env.ARENA_WEB_HOST || '127.0.0.1';
const port = Number(process.env.ARENA_WEB_PORT || settings.port);
const mime = { '.html':'text/html; charset=utf-8', '.css':'text/css; charset=utf-8', '.mjs':'text/javascript; charset=utf-8', '.js':'text/javascript; charset=utf-8', '.json':'application/json; charset=utf-8', '.wasm':'application/wasm', '.png':'image/png', '.jpg':'image/jpeg', '.ttf':'font/ttf', '.txt':'text/plain; charset=utf-8', '.mp4':'video/mp4', '.unityweb':'application/octet-stream', '.data':'application/octet-stream' };
const server = http.createServer((req, res) => {
  if (!['GET','HEAD'].includes(req.method)) { res.writeHead(405, { Allow:'GET, HEAD' }); res.end(); return; }
  let filename;
  try {
    const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    filename = path.resolve(root, '.' + (pathname.endsWith('/') ? pathname + 'index.html' : pathname));
    if (filename !== root && !filename.startsWith(root + path.sep)) throw new Error('outside root');
  } catch { res.writeHead(400); res.end(); return; }
  fs.stat(filename, (error, stat) => {
    if (error || !stat.isFile()) { res.writeHead(404); res.end(); return; }
    // Resolve symlinks too: a packaged build must never serve files outside its own directory.
    let real;
    try { real = fs.realpathSync(filename); }
    catch { res.writeHead(404); res.end(); return; }
    if (!real.startsWith(fs.realpathSync(root) + path.sep)) { res.writeHead(403); res.end(); return; }
    const headers = { 'Content-Type':mime[path.extname(filename)] || 'application/octet-stream', 'Accept-Ranges':'bytes', 'X-Content-Type-Options':'nosniff', 'Cache-Control':'no-cache' };
    let start = 0, end = stat.size - 1, status = 200;
    if (req.headers.range) {
      const match = /^bytes=(\d*)-(\d*)$/.exec(req.headers.range);
      if (!match || (!match[1] && !match[2])) { res.writeHead(416, { 'Content-Range':`bytes */${stat.size}` }); res.end(); return; }
      if (!match[1]) start = Math.max(0, stat.size - Number(match[2]));
      else { start = Number(match[1]); if (match[2]) end = Math.min(end, Number(match[2])); }
      if (start > end || start >= stat.size) { res.writeHead(416, { 'Content-Range':`bytes */${stat.size}` }); res.end(); return; }
      status = 206; headers['Content-Range'] = `bytes ${start}-${end}/${stat.size}`;
    }
    headers['Content-Length'] = end - start + 1;
    res.writeHead(status, headers);
    if (req.method === 'HEAD') { res.end(); return; }
    const stream = fs.createReadStream(filename, { start, end });
    stream.on('error', () => res.destroy()); res.on('close', () => stream.destroy()); stream.pipe(res);
  });
});
server.listen(port, host, () => console.log(`Arena3Q: http://${host}:${port}`));
