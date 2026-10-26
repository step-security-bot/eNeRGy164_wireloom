import { access, readFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('./bin/Release/net10.0/publish/wwwroot/', import.meta.url));
await access(resolve(root, 'index.html'));
const port = Number(process.argv[2] ?? 5180);
const contentTypes = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.json': 'application/json', '.wasm': 'application/wasm' };

createServer(async (request, response) => {
    try {
        const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
        const filename = resolve(root, `.${pathname === '/' ? '/index.html' : pathname}`);
        if (!filename.startsWith(root.endsWith(sep) ? root : root + sep)) {
            response.writeHead(403).end();
            return;
        }

        const content = await readFile(filename);
        response.writeHead(200, {
            'Content-Type': contentTypes[extname(filename)] ?? 'application/octet-stream',
            'Cache-Control': 'no-store',
        });
        response.end(content);
    } catch (error) {
        response.writeHead(error.code === 'ENOENT' ? 404 : 500).end();
    }
}).listen(port, '127.0.0.1', () => {
    console.log(`Wireloom WASM spike: http://127.0.0.1:${port}`);
    console.log(`Serving the published bundle from ${root}`);
});
