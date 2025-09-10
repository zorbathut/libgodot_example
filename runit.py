#!/usr/bin/env python3

import subprocess
import os
import sys

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "init"], check=True)

print("Running scons in the godot directory...")
# extra_suffix is just for compilation optimization, otherwise the binary and libgodot step on each other's feet and cause massively inflated iterative build times
subprocess.run(["scons", "extra_suffix=executable", "dev_build=yes", "debug_symbols=yes"], cwd="godot", check=True)

print("Generating extension API and interface files...")
subprocess.run(["./bin/godot.linuxbsd.editor.dev.x86_64.executable", "--dump-extension-api", "--dump-gdextension-interface", "--headless"], cwd="godot", check=True)

print("Moving generated files to godot-cpp/gdextension/...")
subprocess.run(["mv", "godot/extension_api.json", "godot-cpp/gdextension/extension_api.json"], check=True)
subprocess.run(["mv", "godot/gdextension_interface.h", "godot-cpp/gdextension/gdextension_interface.h"], check=True)

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
env["LD_LIBRARY_PATH"] = os.path.abspath("godot/bin") + ":" + env.get("LD_LIBRARY_PATH", "")
subprocess.run(["./driver/driver"], env=env, check=True)

print("Done!")