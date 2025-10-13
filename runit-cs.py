#!/usr/bin/env python3

import subprocess
import os
import sys
import platform
import shutil

# Platform-specific settings
is_windows = platform.system() == "Windows"
if is_windows:
    godot_exe = os.path.abspath("godot/bin/godot.windows.editor.dev.x86_64.executable.mono.exe")
    driver_exe = "driver-cs/bin/Debug/net8.0/driver-cs.exe"
    lib_path_var = "PATH"
    path_separator = ";"
else:
    godot_exe = "./bin/godot.linuxbsd.editor.dev.x86_64.executable.mono"
    driver_exe = "./driver-cs/bin/Debug/net8.0/driver-cs"
    lib_path_var = "LD_LIBRARY_PATH"
    path_separator = ":"

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("Building Godot executable with Mono support...")
# extra_suffix is just for compilation optimization, otherwise the binary and libgodot step on each other's feet and cause massively inflated iterative build times
# scu_build is just to make the build faster
subprocess.run(["scons", "module_mono_enabled=yes", "extra_suffix=executable", "dev_build=yes", "debug_symbols=yes", "scu_build=yes"], cwd="godot", check=True)

print("Generating Mono glue files...")
subprocess.run([godot_exe, "--headless", "--generate-mono-glue", "./modules/mono/glue"], cwd="godot", check=True)

print("Making NuGet packages directory...")
os.makedirs("godot/bin/GodotSharp/Tools/nupkgs", exist_ok=True)

print("Building C# assemblies and NuGet packages...")
subprocess.run([
        "python",
        "./modules/mono/build_scripts/build_assemblies.py",
        "--godot-output-dir", "./bin",
        "--no-deprecated"
    ], cwd="godot", check=True)

print("Restoring .NET packages...")
subprocess.run(["dotnet", "restore"], cwd="project", check=True)

print("Generating project UID cache...")
subprocess.run([godot_exe, "--path", "../project", "--import", "--headless"], cwd="godot", check=True)

print("Building Godot shared library with Mono support...")
subprocess.run(["scons", "module_mono_enabled=yes", "library_type=shared_library", "extra_suffix=shared_library", "dev_build=yes", "debug_symbols=yes", "scu_build=yes"], cwd="godot", check=True)

print("Building driver-cs...")
subprocess.run(["dotnet", "build"], cwd="driver-cs", check=True)

# Check if --no-run parameter was passed; useful for debugging
if "--no-run" in sys.argv:
    print("Build complete (skipping driver run)!")
    exit(0)

print("Running driver-cs...")
env = os.environ.copy()
lib_path = os.path.abspath("godot/bin")
env[lib_path_var] = lib_path + path_separator + env.get(lib_path_var, "")
subprocess.run([driver_exe], env=env, check=True)

print("Done!")
