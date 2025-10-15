# C# Web Export Implementation Plan

## Architecture Overview

### Initialization Order (CRITICAL)
.NET WASM **must** be initialized first, as it demands to be the host runtime:

```
Browser loads index.html
  └─> JavaScript loads .NET WASM Runtime (dotnet.wasm)
       └─> .NET Runtime starts C# Bootstrap App (driver-cs-web)
            └─> C# Bootstrap uses LibGodot P/Invoke
                 └─> Loads Godot WASM Library (libgodot.wasm)
                      └─> Creates GodotInstance
                           └─> C# manages game loop via iteration()
```

**Key Insight**: Godot must be built as a WASM **library** (not main entry point), similar to how libgodot.so works on Linux. The .NET runtime is the actual main module.

## Implementation Phases

### Phase 1: Build Script (runit-cs-webexport.py)

Create `runit-cs-webexport.py` that:

1. **Initialize submodules** (like existing scripts)
2. **Build Godot editor with Mono** for glue generation
   - `scons module_mono_enabled=yes extra_suffix=executable`
3. **Generate Mono glue**
   - `godot --headless --generate-mono-glue`
4. **Build C# assemblies**
   - Run `modules/mono/build_scripts/build_assemblies.py`
5. **Build Godot as WASM library with Mono**
   - `scons platform=web library_type=shared_library module_mono_enabled=yes`
   - This is the critical step - needs godot/platform/web changes first
6. **Build C# driver-cs-web project**
   - `dotnet publish -c Release` targeting browser-wasm
7. **Package everything** into build/web/

### Phase 2: Enable Godot Library Mode for Web

#### File: `godot/platform/web/detect.py`
- Line 70: Add `"mono"` to supported features: `"supported": ["mono"]`
- Ensure library_type configurations work for web platform

#### File: `godot/platform/web/libgodot_web.cpp` (NEW)
Create web implementation of libgodot API (modeled on `platform/linuxbsd/libgodot_linuxbsd.cpp`):
- Implement `libgodot_create_godot_instance()`
- Implement `libgodot_destroy_godot_instance()`
- Create OS_Web instance
- Call Main::setup() with p_init_func callback
- Return GodotInstance pointer

#### File: `godot/platform/web/export/export_plugin.cpp`
- Lines 420-424: Remove/conditionalize the C# blocking error
- Allow C# web exports when templates are available

#### File: `godot/modules/mono/config.py`
- Update `can_build()` to allow web platform
- May need platform-specific checks for .NET WASM runtime requirements

#### File: `godot/platform/web/SCsub`
- Add conditional compilation of `libgodot_web.cpp` when `module_mono_enabled=yes`
- Handle Mono-specific link flags for web

### Phase 3: C# Web Driver Bootstrap

#### Directory: `driver-cs-web/`

**File: `driver-cs-web.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="GodotSharp">
      <HintPath>../godot/bin/GodotSharp/Api/Release/GodotSharp.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

**File: `driver-cs-web/Program.cs`**
Adapt `driver-cs/Program.cs` but for web:
- Export a JavaScript-callable entry point
- Use P/Invoke to call libgodot functions in Godot WASM
- Manage main loop via requestAnimationFrame integration
- Handle web-specific initialization (no file paths, different args)

**File: `driver-cs-web/LibGodotWeb.cs`**
Adapt `driver-cs/LibGodot.cs`:
- Change DllImport to reference the Godot WASM module name
- Keep same P/Invoke signatures (GDExtension interface is platform-agnostic)
- Handle WASM-specific marshaling if needed

**File: `driver-cs-web/index.html` (template)**
HTML template that:
- Loads dotnet.wasm runtime
- Configures .NET to load driver-cs-web.dll
- Provides canvas for Godot rendering
- Includes proper COOP/COEP headers via meta tags

### Phase 4: Web Export Template Integration

#### JavaScript Bootstrap
Create `godot/platform/web/js/libs/library_godot_dotnet.js`:
- Load .NET WASM runtime first
- Initialize C# bootstrap
- Pass control to C# which will load Godot WASM

#### Emscripten Build Configuration
When `module_mono_enabled=yes`:
- Build Godot as a side module that .NET can load
- Export libgodot functions with `EMSCRIPTEN_KEEPALIVE`
- Configure memory sharing between .NET WASM and Godot WASM

#### File: `godot/platform/web/web_main.cpp`
May need modifications if C# bootstrap is managing the entry point instead of godot_web_main().

### Phase 5: Testing Strategy

1. **Unit test**: Verify Godot builds as WASM library with Mono
2. **Integration test**: Verify C# bootstrap can load and call libgodot
3. **Full test**: Export a simple C# project and run in browser
4. **Compatibility test**: Ensure GDScript-only projects still work

## Technical Challenges & Solutions

### Challenge 1: Dual WASM Modules
**Problem**: .NET WASM and Godot WASM need to coexist
**Solution**: Build Godot as a dynamic library that .NET can dlopen/load

### Challenge 2: Memory Sharing
**Problem**: C# and Godot need to share memory for GDExtension interface
**Solution**: Use Emscripten's dynamic linking with shared memory

### Challenge 3: Build Size
**Problem**: .NET runtime + Godot + game code is large
**Solution**: Use .NET native AOT if available, enable aggressive size optimization

### Challenge 4: GDExtension on Web
**Problem**: GDExtension wasn't designed for dual-WASM scenario
**Solution**: Ensure GDExtension interface pointers work across WASM module boundaries

## Implementation Order (Recommended)

1. ✅ **Research phase complete** - understand architecture
2. **Write `runit-cs-webexport.py`** - even if build fails, script is ready
3. **Add web libgodot implementation** - `godot/platform/web/libgodot_web.cpp`
4. **Update platform detection** - add "mono" support flags for web
5. **Test Godot build** - `scons platform=web library_type=shared_library module_mono_enabled=yes`
6. **Fix build errors** - iterate on Godot platform/module config
7. **Create driver-cs-web project** - basic C# bootstrap
8. **Test local WASM loading** - verify C# can load Godot WASM
9. **Integrate into export template** - wire up JavaScript/HTML
10. **End-to-end test** - export and run a C# game in browser

## Key Files Reference

**Existing files to study:**
- `driver-cs/Program.cs` - Desktop C# bootstrap pattern
- `driver-cs/LibGodot.cs` - LibGodot P/Invoke bindings
- `godot/platform/linuxbsd/libgodot_linuxbsd.cpp` - Native libgodot implementation
- `godot/platform/web/web_main.cpp` - Current web entry point
- `runit-cs.py` - C# build script pattern
- `runit-webexport.py` - Web export build pattern

**New files to create:**
- `runit-cs-webexport.py` - Combined build script
- `driver-cs-web/` - Entire directory
- `godot/platform/web/libgodot_web.cpp` - Web libgodot implementation

**Files to modify:**
- `godot/platform/web/detect.py` - Add mono support
- `godot/platform/web/SCsub` - Add libgodot build
- `godot/platform/web/export/export_plugin.cpp` - Remove C# block
- `godot/modules/mono/config.py` - Allow web platform

## Notes for Multiple Sessions

- Each phase can be tackled independently
- Start with Phase 2 (Godot changes) before Phase 3 (C# driver)
- Test incrementally - build Godot WASM library before attempting C# integration
- If stuck, reference how driver-cs works on Linux as a template
- The core pattern is: .NET hosts, C# bootstraps, LibGodot loads Godot as library
