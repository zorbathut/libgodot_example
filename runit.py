#!/usr/bin/env python3

import subprocess
import os
import sys
import platform
import shutil

# Platform-specific settings
is_windows = platform.system() == "Windows"
if is_windows:
    godot_exe = "godot/bin/godot.windows.editor.dev.x86_64.executable.exe"
    driver_exe = "driver/driver.exe"
    lib_path_var = "PATH"
    path_separator = ";"
else:
    godot_exe = "./bin/godot.linuxbsd.editor.dev.x86_64.executable"
    driver_exe = "./driver/driver"
    lib_path_var = "LD_LIBRARY_PATH"
    path_separator = ":"

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("Running scons in the godot directory...")
# extra_suffix is just for compilation optimization, otherwise the binary and libgodot step on each other's feet and cause massively inflated iterative build times
subprocess.run(["scons", "extra_suffix=executable", "dev_build=yes", "debug_symbols=yes"], cwd="godot", check=True)

print("Generating extension API and interface files...")
subprocess.run([godot_exe, "--dump-extension-api", "--dump-gdextension-interface", "--headless"], cwd="godot", check=True)

print("Moving generated files to godot-cpp/gdextension/...")
shutil.move("godot/extension_api.json", "godot-cpp/gdextension/extension_api.json")
shutil.move("godot/gdextension_interface.h", "godot-cpp/gdextension/gdextension_interface.h")

print("Running scons in the godot-cpp directory...")
subprocess.run(["scons", "debug_symbols=yes", "dev_build=yes", "optimize=debug"], cwd="godot-cpp", check=True)

print("Building godot with shared library...")
subprocess.run(["scons", "library_type=shared_library", "extra_suffix=shared_library", "dev_build=yes", "debug_symbols=yes"], cwd="godot", check=True)

print("Building driver...")
subprocess.run(["scons"], cwd="driver", check=True)

# Check if --no-run parameter was passed
if "--no-run" in sys.argv:
    print("Build complete (skipping driver run)!")
    exit(0)

print("Running driver...")
env = os.environ.copy()
lib_path = os.path.abspath("godot/bin")
env[lib_path_var] = lib_path + path_separator + env.get(lib_path_var, "")
subprocess.run([driver_exe], env=env, check=True)

print("Done!")