# LibGodot C# Web Export — Session Handoff

## What This Project Does

This project embeds the Godot game engine as a library inside a C# application. The `csharp` branch has a working native Linux version. The `webex` branch (current) is getting the web/WASM export working.

Architecture: **Browser → .NET WASM runtime → C# bootstrap (driver-cs-web) → P/Invoke → Godot WASM static library → GodotInstance**

## Current State (webex branch, commit c5a167c)

**The C# → WASM → Godot pipeline is fully working.** Specifically:

1. .NET WASM runtime loads and initializes in the browser
2. `project.pck` is preloaded into emscripten filesystem via `Module.copyToFS`
3. C# code runs: `GodotLauncher.InitializeGodot()` is called from JavaScript
4. `libgodot_create_godot_instance()` P/Invoke successfully creates a Godot instance
5. GDExtension interface is loaded via C wrapper functions (object_get_instance_id, classdb_get_method_bind, etc.)
6. `GodotInstance::start()` is called and Godot Engine initializes
7. Godot sets the browser tab title to the project name ("project")

**Current blocker**: The test VM has no real GPU (uses llvmpipe software renderer). WebGL2 is not available, so Godot's DisplayServerWeb fails to create a GL context. This produces browser alert dialogs that freeze the page. **This needs to be tested on a machine with a real GPU.**

There may also be a **page-blocking issue** — `GodotInstance::start()` might block the main thread synchronously. On native Linux, `start()` returns and then `iteration()` is called in a loop. On web, if `start()` blocks, the browser freezes. This needs investigation once WebGL2 is available.

## Key Technical Discoveries (IMPORTANT for next session)

### WASM Interop Constraints

1. **`Marshal.GetFunctionPointerForDelegate` does NOT work on WASM.** You get `PlatformNotSupportedException`. All native→managed callbacks must use `[UnmanagedCallersOnly]` with function pointer syntax: `(IntPtr)(delegate* unmanaged[Cdecl]<...>)&MethodName`

2. **The `[UnmanagedCallersOnly]` InitCallback must be absolutely minimal.** Any complex operations inside it (DllImport calls, `&` address-of other methods, Console.WriteLine is fine but Marshal operations crash) cause `interp.c:2341` assertion failures. The Mono WASM interpreter can't handle these in the native→managed callback context.

3. **Raw function pointer calls (`delegate* unmanaged[Cdecl]`) crash on WASM** with `CANNOT HANDLE INTERP ICALL SIG` from `aot-runtime-wasm.c:187`. The Mono interpreter needs pre-registered trampolines for each function signature. Standard `[DllImport]` P/Invoke has these auto-generated, but raw function pointer casts don't.

4. **Solution: C wrapper functions.** `gdextension_wrappers.c` contains thin C functions that call the GDExtension function pointers. C# calls these via `[DllImport]`. This gives the Mono interpreter proper P/Invoke trampolines. The C file is included via `<NativeFileReference>` in the csproj.

5. **`DllImport("__Internal")` does NOT work** for user C files on .NET 9 WASM. Instead, use the same library name as the Godot static library (`libgodot.web.template_release.wasm32.nothreads.cs_webexport`) — everything is linked into one WASM module anyway.

6. **Missing WASM symbol exports** — `_sbrk`, `_free`, `_memalign`, etc. must be in `EXPORTED_FUNCTIONS` in the csproj's `EmccExtraLDFlags`, otherwise the .NET runtime crashes at startup.

### Emscripten Version Matching

- Godot requires emscripten >= 4.0.0, BUT the fork whitelists 3.1.56 specifically
- .NET 9's `wasm-tools` workload bundles emscripten 3.1.56
- **Both Godot and .NET must use the same emscripten version** or you get `__wasm_setjmp` link errors
- System emsdk must be set to 3.1.56: `cd ~/emsdk && ./emsdk install 3.1.56 && ./emsdk activate 3.1.56`

### Godot Fork

- Submodule points to `zorbathut/godot` commit `12dd44a4df` (branch `webex`)
- This is based on `pr/libgodot_cs` (the libgodot C# patches) plus 5 web-specific commits
- Web patches add: `libgodot_web.cpp`, SCsub library_type support, detect.py emscripten whitelist + library/mono support, export blocker removal
- The fork is based on Godot ~4.6-dev, NOT 4.6-stable

## Build Instructions

### Prerequisites
```bash
# .NET 9
~/.dotnet/dotnet --version  # should be 9.x
dotnet workload install wasm-tools

# Emscripten 3.1.56
cd ~/emsdk && ./emsdk activate 3.1.56 && source emsdk_env.sh

# scons
~/.local/bin/scons --version

# PATH setup
export PATH="$HOME/.dotnet:$HOME/.local/bin:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
source ~/emsdk/emsdk_env.sh
```

### Full Build
```bash
python3 runit-cs-webexport.py
```
This script does everything: builds native Godot editor, generates glue, builds C# assemblies, builds web export templates, exports .pck, builds Godot WASM static library, and runs `dotnet publish`. It has a `sys.exit(0)` at line 186 ("gotta do more here") that stops before the assembly/serve phase.

### Quick Rebuild (after initial build)
```bash
# Just rebuild the C# web driver (fast, ~60 seconds)
rm -rf driver-cs-web/bin driver-cs-web/obj
dotnet publish driver-cs-web/ -c Release
cp build/export-temp/index.pck driver-cs-web/bin/Release/net9.0/publish/wwwroot/project.pck

# Serve
cd driver-cs-web/bin/Release/net9.0/publish/wwwroot
python3 -c "
import http.server, socketserver
class H(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        self.send_header('Cache-Control', 'no-cache, no-store, must-revalidate')
        super().end_headers()
socketserver.TCPServer(('', 8080), H).serve_forever()
"
```

## Key Files

| File | Purpose |
|------|---------|
| `driver-cs-web/LibGodotWeb.cs` | P/Invoke bindings, GDExtension interface loading via C wrappers |
| `driver-cs-web/GodotLauncher.cs` | JSExport entry points: InitializeGodot, RunFrame, Shutdown |
| `driver-cs-web/gdextension_wrappers.c` | C wrappers for GDExtension function pointer calls (WASM trampoline workaround) |
| `driver-cs-web/driver-cs-web.csproj` | WebAssembly project config, NativeFileReference, EmccExtraLDFlags |
| `driver-cs-web/wwwroot/index.html` | Browser bootstrap: loads .NET, preloads .pck, configures canvas, calls InitializeGodot |
| `driver-cs-web/wwwroot/main.js` | requestAnimationFrame loop (not yet tested - page freezes before getting there) |
| `runit-cs-webexport.py` | Full build pipeline script |
| `godot/platform/web/libgodot_web.cpp` | Web platform libgodot entry point (in the Godot fork) |

## Next Steps

1. **Test on a machine with WebGL2** — the code should work, the VM just lacks GPU support
2. **Fix the `initialization.initialize is null` warning** — the InitCallback currently sets initialize/deinitialize to IntPtr.Zero because setting them via `&` crashes. Need to either:
   - Set them in the C wrapper instead of in C#
   - Or find a way to pass [UnmanagedCallersOnly] function pointers that doesn't crash the interpreter
3. **Verify `GodotInstance::start()` doesn't block the main thread** — if it does, may need to restructure the initialization flow
4. **Get the frame loop working** — `RunFrame()` calls `godotInstance.Iteration()` via requestAnimationFrame
5. **Handle Godot's Mono module initialization** — Godot's `gd_mono.cpp` will try to load hostfxr/coreclr, which won't exist on web. This may need web-specific handling (early return or connecting to the existing .NET runtime)
6. **Update Godot fork to 4.6.1 stable** — the current fork is on 4.6-dev
7. **Update to .NET 10** — for better WASM support and potentially newer emscripten

## What NOT to Do

- Don't use `Marshal.GetFunctionPointerForDelegate` — it's broken on WASM
- Don't call native function pointers directly from C# via `delegate*` — use C wrapper functions instead
- Don't do complex operations inside `[UnmanagedCallersOnly]` callbacks — keep them minimal
- Don't use `DllImport("__Internal")` for user C functions — use the Godot library name
- Don't build Godot web targets with a different emscripten than .NET's wasm-tools bundles
