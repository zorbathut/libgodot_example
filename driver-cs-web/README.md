# driver-cs-web

C# Bootstrap for Godot Web Export with Mono support.

## Architecture

This project provides the C# entry point for running Godot games compiled with C# support in the browser:

```
Browser → .NET WASM Runtime → driver-cs-web (this project) → LibGodot P/Invoke → Godot WASM
```

## How It Works

1. **HTML loads .NET WASM runtime first** (`dotnet.js`, `dotnet.wasm`)
2. **.NET runtime loads driver-cs-web.dll** (this project, compiled to WASM)
3. **JavaScript calls `InitializeGodot()`** exposed via `[JSExport]`
4. **C# calls `libgodot_create_godot_instance()`** via P/Invoke to load Godot WASM as a library
5. **C# manages the game loop** via `GodotInstance.iteration()` called each frame

## Building

```bash
# From the driver-cs-web directory
dotnet publish -c Release
```

This will create the publish output in `bin/Release/net8.0/browser-wasm/publish/`

## Files

- **driver-cs-web.csproj** - Project file targeting browser-wasm
- **Program.cs** - Main entry point with `[JSExport]` methods
- **LibGodotWeb.cs** - P/Invoke bindings to Godot WASM library
- **main.js** - JavaScript module for animation frame loop
- **index.html** - HTML template that loads everything

## Integration

To use this with a Godot project:

1. Build driver-cs-web: `dotnet publish -c Release`
2. Copy publish output to your web export directory
3. Copy Godot WASM files (from `godot/bin/godot.web.*.mono.wasm`)
4. Copy Godot C# assemblies (GodotSharp.dll, etc.)
5. Update paths in index.html if needed
6. Serve with a web server that provides proper COOP/COEP headers

## Status

**Current**: Proof of concept / work in progress
**Next**: Manual assembly and testing
**Future**: Integration into Godot's web export template system
