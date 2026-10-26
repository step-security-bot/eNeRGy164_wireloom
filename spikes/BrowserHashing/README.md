# Browser hashing spike

This executable spike compiles a fixed `@autoid(HASH)` IDL in .NET WebAssembly.
The browser calls the shared compiler through `[JSExport]`. Each compilation
supplies a synchronous `[JSImport]` callback to a JavaScript MD5 implementation.
The shared compiler still owns UTF-8 encoding and the DDS member-ID conversion.

The page checks three literal expected member IDs for both RTI profiles:
`color` → `262528368`, `shapesize` → `74944730`, and the explicit UTF-8
`@hashid("é")` → `130932070`. It displays the runtime, callback count,
diagnostics, and complete generated C# documents. **Compile again** repeats the
same compilation after initialization.

## Run

Use the SDK selected by the repository's `global.json` and Node.js. The browser
runtime is pinned to .NET `10.0.13`. This spike uses the interpreter with native
relinking and AOT disabled; the optional `wasm-tools` workload is not required
for this configuration.

From the repository root:

```powershell
dotnet restore spikes/BrowserHashing/Wireloom.Dds.BrowserSpike.csproj --locked-mode
dotnet publish spikes/BrowserHashing/Wireloom.Dds.BrowserSpike.csproj --configuration Release --no-restore
node spikes/BrowserHashing/serve.mjs
```

Open <http://127.0.0.1:5180>. The server binds to the local machine and serves
the published `bin/Release/net10.0/publish/wwwroot` bundle. Stop it with Ctrl+C.
An optional port can be passed after `serve.mjs`.

Run the provider checks separately:

```powershell
node --test spikes/BrowserHashing/md5.test.mjs
```

These check the seven RFC 1321 vectors and native MD5 parity for the sample,
Unicode, MD5 padding boundaries, binary input, repeated calls, and unchanged
input buffers.

## Observed result

The Release-published bundle passed in the Codex in-app browser on Windows on
October 26, 2026, using SDK `10.0.402` and browser runtime `.NET 10.0.13`.
Both profiles produced four documents, no diagnostics, three callback calls,
and all three expected IDs. Repeating compilation passed. The 21 provider
checks also passed under Node.js.

## Scope

The project references only `Wireloom.Dds.Compiler`; it needs no Roslyn or RTI
runtime. The Microsoft browser SDK/runtime packs come from the repository's
existing approved NuGet.org source. No npm package or third-party MD5 source
is imported: `wwwroot/md5.mjs` is a small experimental implementation authored
from the algorithm in [RFC 1321, section 3](https://www.rfc-editor.org/rfc/rfc1321#section-3).
MD5 serves DDS protocol identity here and is not intended for security use.

This project is isolated from the main solution and generator package. It is
a proof of the synchronous browser callback, rather than full qualification
of [issue #108](https://github.com/eNeRGy164/wireloom/issues/108). Full corpus
parity, browser CI, worker scheduling, and provider selection for a maintained
browser application remain separate work. The app follows the official
[.NET browser interop model](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/wasm-browser-app?view=aspnetcore-10.0).
