import { dotnet } from './_framework/dotnet.js';
import { computeMd5 } from './md5.mjs';

const status = document.querySelector('#status');
const button = document.querySelector('#run');

try {
    const runtime = await dotnet.create();
    runtime.setModuleImports('hash-provider', { computeMd5 });
    const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);

    function run() {
        const report = JSON.parse(exports.Wireloom.BrowserSpike.Run());
        const passed = report.isBrowser && report.profiles.every(profile => profile.success);
        status.textContent = passed ? 'PASS — compiled in WebAssembly; all member IDs match.' : 'FAIL — inspect the checks below.';
        status.dataset.result = passed ? 'pass' : 'fail';
        document.querySelector('#idl').textContent = report.idl;
        document.querySelector('#report').textContent = JSON.stringify({
            isBrowser: report.isBrowser,
            runtime: report.runtime,
            provider: 'Synchronous JavaScript MD5 via [JSImport]',
            profiles: report.profiles.map(({ documents, ...profile }) => ({ ...profile, documents: documents.length })),
        }, null, 2);

        const documents = document.querySelector('#documents');
        documents.replaceChildren();
        for (const profile of report.profiles) {
            for (const generatedDocument of profile.documents) {
                const details = document.createElement('details');
                const summary = document.createElement('summary');
                const source = document.createElement('pre');
                summary.textContent = `${profile.profile} / ${generatedDocument.name}`;
                source.textContent = generatedDocument.source;
                details.append(summary, source);
                documents.append(details);
            }
        }
    }

    button.addEventListener('click', run);
    button.disabled = false;
    run();
} catch (error) {
    status.textContent = `FAIL — ${error.message}`;
    status.dataset.result = 'fail';
    console.error(error);
}
