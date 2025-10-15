#!/usr/bin/env python3

import subprocess
import os
import sys
import platform
import shutil
import multiprocessing
import http.server
import socketserver

# Check if Emscripten is available
def check_emscripten():
    try:
        result = subprocess.run(["emcc", "--version"], capture_output=True, text=True)
        if result.returncode == 0:
            print(f"Found Emscripten: {result.stdout.splitlines()[0]}")
            return True
    except FileNotFoundError:
        pass

    print("ERROR: Emscripten not found in PATH!")
    print("Web export requires Emscripten 3.1.62 or later.")
    print("\nTo install Emscripten:")
    print("  1. git clone https://github.com/emscripten-core/emsdk.git")
    print("  2. cd emsdk")
    print("  3. ./emsdk install latest")
    print("  4. ./emsdk activate latest")
    print("  5. source ./emsdk_env.sh  (or run emsdk_env.bat on Windows)")
    print("\nThen run this script again.")
    return False

# Platform-specific settings
is_windows = platform.system() == "Windows"
if is_windows:
    godot_exe = os.path.abspath("godot/bin/godot.windows.editor.dev.x86_64.executable.mono.exe")
    lib_path_var = "PATH"
    path_separator = ";"
else:
    godot_exe = "./bin/godot.linuxbsd.editor.dev.x86_64.executable.mono"
    lib_path_var = "LD_LIBRARY_PATH"
    path_separator = ":"

cores = multiprocessing.cpu_count()
print(f"Building with {cores} cores")

# Check for Emscripten
if not check_emscripten():
    sys.exit(1)

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("=" * 60)
print("PHASE 1: Build native Godot editor with Mono support")
print("=" * 60)
# extra_suffix to avoid conflicts with other builds
subprocess.run([
    "scons",
    "-j", f"{cores}",
    "module_mono_enabled=yes",
    "extra_suffix=executable",
    "dev_build=yes",
    "debug_symbols=yes",
    "scu_build=yes"
], cwd="godot", check=True)

print("=" * 60)
print("PHASE 2: Generate Mono glue files")
print("=" * 60)
subprocess.run([godot_exe, "--headless", "--generate-mono-glue", "./modules/mono/glue"], cwd="godot", check=True)

print("=" * 60)
print("PHASE 3: Create NuGet packages directory")
print("=" * 60)
os.makedirs("godot/bin/GodotSharp/Tools/nupkgs", exist_ok=True)

print("=" * 60)
print("PHASE 4: Build C# assemblies and NuGet packages")
print("=" * 60)
subprocess.run([
    "python",
    "./modules/mono/build_scripts/build_assemblies.py",
    "--godot-output-dir", "./bin",
    "--no-deprecated"
], cwd="godot", check=True)

print("=" * 60)
print("PHASE 5: Restore .NET packages for project")
print("=" * 60)
subprocess.run(["dotnet", "restore"], cwd="project", check=True)

print("=" * 60)
print("PHASE 6: Generate project UID cache")
print("=" * 60)
subprocess.run([godot_exe, "--path", "../project", "--import", "--headless"], cwd="godot", check=True)

print("=" * 60)
print("PHASE 7: Build Godot as WASM library with Mono support")
print("=" * 60)
print("NOTE: This will likely fail until Godot platform code is updated!")
print("=" * 60)
# Build Godot for web as a shared library with Mono enabled
# extra_suffix to avoid conflicts with regular web builds
try:
    subprocess.run([
        "scons",
        "-j", f"{cores}",
        "platform=web",
        "library_type=shared_library",
        "module_mono_enabled=yes",
        "target=template_release",
        "extra_suffix=cs_webexport",
        "optimize=size"
    ], cwd="godot", check=True)
    print("SUCCESS: Godot WASM library built!")
except subprocess.CalledProcessError as e:
    print("=" * 60)
    print("EXPECTED FAILURE: Web platform doesn't support Mono yet")
    print("Next steps:")
    print("  1. Modify godot/platform/web/detect.py to add 'mono' support")
    print("  2. Create godot/platform/web/libgodot_web.cpp")
    print("  3. Update godot/modules/mono/config.py to allow web platform")
    print("  4. Re-run this script")
    print("=" * 60)
    sys.exit(1)

print("=" * 60)
print("PHASE 8: Build driver-cs-web (C# Bootstrap)")
print("=" * 60)
subprocess.run(["dotnet", "publish", "-c", "Release"], cwd="driver-cs-web", check=True)

# Create output directory
output_dir = "build/web-cs"
if os.path.exists(output_dir):
    print(f"Cleaning existing build directory: {output_dir}")
    shutil.rmtree(output_dir)
os.makedirs(output_dir, exist_ok=True)

print("=" * 60)
print("PHASE 9: Assemble web export manually")
print("=" * 60)

# Copy driver-cs-web publish output
driver_publish = "driver-cs-web/bin/Release/net8.0/browser-wasm/publish"
print(f"Copying driver-cs-web from {driver_publish}...")
for item in os.listdir(driver_publish):
    src = os.path.join(driver_publish, item)
    dst = os.path.join(output_dir, item)
    if os.path.isfile(src):
        shutil.copy2(src, dst)
    elif os.path.isdir(src):
        shutil.copytree(src, dst, dirs_exist_ok=True)

# Copy Godot WASM files
print("Copying Godot WASM library...")
godot_wasm_base = "godot/bin/godot.web.template_release.wasm32.cs_webexport.mono"
shutil.copy2(f"{godot_wasm_base}.wasm", f"{output_dir}/godot.wasm")
shutil.copy2(f"{godot_wasm_base}.js", f"{output_dir}/godot.js")

# Copy Godot C# assemblies
print("Copying GodotSharp assemblies...")
godot_sharp_dir = os.path.join(output_dir, "GodotSharp")
os.makedirs(godot_sharp_dir, exist_ok=True)
shutil.copytree("godot/bin/GodotSharp", godot_sharp_dir, dirs_exist_ok=True)

# Copy project files (we'll need to handle this differently for web)
print("Copying project files...")
project_dir = os.path.join(output_dir, "project")
os.makedirs(project_dir, exist_ok=True)
# For now, just copy the .pck file if export created one
# Later we'll handle filesystem mounting properly

print(f"\n{'=' * 60}")
print("Assembly complete!")
print(f"{'=' * 60}")
print(f"Output directory: {output_dir}/")

# Check if --no-run parameter was passed
if "--no-run" in sys.argv:
    print("\n" + "="*60)
    print("Build complete (skipping server)!")
    print("="*60)
    print(f"\nWeb build output: {output_dir}/")
    print("\nTo test manually:")
    print("  1. cd build/web-cs")
    print("  2. python -m http.server 8088")
    print("  3. Open http://localhost:8088 in your browser")
    print("  4. Check browser console for C# and Godot output")
    exit(0)

print("\n" + "="*60)
print("Build complete! Starting web server...")
print("="*60)
print(f"\nWeb build output: {output_dir}/")

# Serve the web build with proper COOP/COEP headers
PORT = 8088

class CORSRequestHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Required headers for SharedArrayBuffer/WebAssembly
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        super().end_headers()

print(f"\nStarting web server on http://localhost:{PORT}")
print(f"Open http://localhost:{PORT}/index.html in your browser to test")
print("Press Ctrl+C to stop the server")
print()

# Change to the web directory
os.chdir(output_dir)

with socketserver.TCPServer(("", PORT), CORSRequestHandler) as httpd:
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nServer stopped.")
        print("Done!")
