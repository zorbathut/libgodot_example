#!/usr/bin/env python3

import subprocess
import os
import sys
import platform
import shutil

# Platform-specific settings
is_windows = platform.system() == "Windows"
if is_windows:
    godot_exe = os.path.abspath("godot/bin/godot.windows.editor.dev.x86_64.executable.exe")
    driver_exe = "driver-cpp-static/driver.exe"
else:
    godot_exe = "./bin/godot.linuxbsd.editor.dev.x86_64.executable"
    driver_exe = "./driver-cpp-static/driver"

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("Building Godot executable...")
# extra_suffix is just for compilation optimization, otherwise the binary and libgodot step on each other's feet and cause massively inflated iterative build times
# scu_build is just to make the build faster
subprocess.run(["scons", "extra_suffix=executable", "dev_build=yes", "debug_symbols=yes", "scu_build=yes"], cwd="godot", check=True)

print("Generating project UID cache...")
subprocess.run([godot_exe, "--path", "../project", "--import", "--headless"], cwd="godot", check=True)

print("Generating extension API and interface files...")
subprocess.run([godot_exe, "--dump-extension-api", "--dump-gdextension-interface", "--headless"], cwd="godot", check=True)

print("Moving generated files to godot-cpp/gdextension/...")
shutil.move("godot/extension_api.json", "godot-cpp/gdextension/extension_api.json")
shutil.move("godot/gdextension_interface.h", "godot-cpp/gdextension/gdextension_interface.h")

print("Building GDExtension bindings...")
subprocess.run(["scons", "debug_symbols=yes", "dev_build=yes", "optimize=debug"], cwd="godot-cpp", check=True)

print("Building Godot static library...")
subprocess.run(["scons", "library_type=static_library", "extra_suffix=static_library", "dev_build=yes", "debug_symbols=yes", "scu_build=yes"], cwd="godot", check=True)

print("Building static driver...")
subprocess.run(["scons"], cwd="driver-cpp-static", check=True)

# Check if --no-run parameter was passed; useful for debugging
if "--no-run" in sys.argv:
    print("Build complete (skipping driver run)!")
    exit(0)

print("Running static driver...")
# No need to set LD_LIBRARY_PATH for statically linked executable
subprocess.run([driver_exe], check=True)

print("Done!")
