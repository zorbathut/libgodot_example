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

# TODO: Phase 8 - Build driver-cs-web (will add after Godot changes work)

# Create output directory
output_dir = "build/web-cs"
if os.path.exists(output_dir):
    print(f"Cleaning existing build directory: {output_dir}")
    shutil.rmtree(output_dir)
os.makedirs(output_dir, exist_ok=True)

print("=" * 60)
print("PHASE 8: Export project for web")
print("=" * 60)
# Use the editor we built to export the project
subprocess.run([
    godot_exe,
    "--headless",
    "--path", "../project",
    "--export-release", "Web",
    f"../{output_dir}/index.html"
], cwd="godot", check=True)

# Check if --no-run parameter was passed
if "--no-run" in sys.argv:
    print("\n" + "="*60)
    print("Build complete (skipping server)!")
    print("="*60)
    print(f"\nWeb build output: {output_dir}/")
    exit(0)

print("\n" + "="*60)
print("Build complete!")
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
