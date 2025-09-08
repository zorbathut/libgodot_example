#!/usr/bin/env python3

import subprocess

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "update", "--init", "--recursive", "--progress"])

print("Running scons in the godot directory...")
# extra_suffix is just for compilation optimization, otherwise the binary and libgodot step on each other's feet and cause massively inflated iterative build times
subprocess.run(["scons", "extra_suffix=executable"], cwd="godot")

print("Generating extension API and interface files...")
subprocess.run(["./bin/godot.linuxbsd.editor.x86_64.executable", "--dump-extension-api", "--dump-gdextension-interface", "--headless"], cwd="godot")

print("Moving generated files to godot-cpp/gdextension/...")
subprocess.run(["mv", "godot/extension_api.json", "godot-cpp/gdextension/extension_api.json"])
subprocess.run(["mv", "godot/gdextension_interface.h", "godot-cpp/gdextension/gdextension_interface.h"])

print("Running scons in the godot-cpp directory...")
subprocess.run(["scons"], cwd="godot-cpp")

print("Building godot with shared library...")
subprocess.run(["scons", "library_type=shared_library", "extra_suffix=shared_library"], cwd="godot")

print("Building driver...")
subprocess.run(["scons"], cwd="driver")

print("Running driver...")
import os
env = os.environ.copy()
env["LD_LIBRARY_PATH"] = os.path.abspath("godot/bin") + ":" + env.get("LD_LIBRARY_PATH", "")
subprocess.run(["./driver"], cwd="driver", env=env)

print("Done!")