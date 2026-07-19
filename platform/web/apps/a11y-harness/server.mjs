import { createReadStream, existsSync, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, normalize } from 'node:path';

const root = normalize(join(import.meta.dirname, '..', '..'));
const types = new Map([['.html', 'text/html; charset=utf-8'], ['.js', 'text/javascript; charset=utf-8'], ['.css', 'text/css; charset=utf-8']]);
createServer((request, response) => {
  const path = normalize(join(root, decodeURIComponent(new URL(request.url ?? '/', 'http://localhost').pathname)));
  if (!path.startsWith(root) || !existsSync(path) || !statSync(path).isFile()) {
    response.writeHead(404).end('Not found');
    return;
  }
  response.writeHead(200, { 'Content-Type': types.get(extname(path)) ?? 'application/octet-stream' });
  createReadStream(path).pipe(response);
}).listen(4175, '127.0.0.1');
