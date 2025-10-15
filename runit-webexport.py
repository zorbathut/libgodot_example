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
    godot_exe = os.path.abspath("godot/bin/godot.windows.editor.dev.x86_64.executable.exe")
else:
    godot_exe = "./bin/godot.linuxbsd.editor.dev.x86_64.executable"

cores = multiprocessing.cpu_count()
print(f"Building with {cores} cores")

# Check for Emscripten
if not check_emscripten():
    sys.exit(1)

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("Building Godot editor for web export process...")
# We need the editor to run the export process
# extra_suffix is for compilation optimization to avoid conflicts
subprocess.run([
    "scons",
    "-j", f"{cores}",
    "extra_suffix=executable",
    "dev_build=yes",
    "debug_symbols=yes",
    "scu_build=yes"
], cwd="godot", check=True)

print("Generating project UID cache...")
subprocess.run([godot_exe, "--path", "../project", "--import", "--headless"], cwd="godot", check=True)

print("Building web export template (release)...")
# Build the release template for web
subprocess.run([
    "scons",
    "-j", f"{cores}",
    "platform=web",
    "target=template_release",
    "optimize=size",  # Web builds benefit from size optimization
    "extra_suffix=webexport",
], cwd="godot", check=True)

print("Building web export template (debug)...")
# Build the debug template for web - useful for testing
subprocess.run([
    "scons",
    "-j", f"{cores}",
    "platform=web",
    "target=template_debug",
], cwd="godot", check=True)

# Create output directory
output_dir = "build/web"
if os.path.exists(output_dir):
    print(f"Cleaning existing build directory: {output_dir}")
    shutil.rmtree(output_dir)
os.makedirs(output_dir, exist_ok=True)

print("Exporting project for web...")
# Use the editor we built to export the project
subprocess.run([
    godot_exe,
    "--headless",
    "--path", "../project",
    "--export-release", "Web",
    f"../{output_dir}/index.html"
], cwd="godot", check=True)

# Check if --no-run parameter was passed; useful for debugging
if "--no-run" in sys.argv:
    print("\n" + "="*60)
    print("Web export complete (skipping server)!")
    print("="*60)
    print(f"\nWeb build output: {output_dir}/")
    exit(0)

print("\n" + "="*60)
print("Web export complete!")
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
