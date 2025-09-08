#!/usr/bin/env python3

import subprocess

print("Initializing Git submodules...")
subprocess.run(["git", "submodule", "update", "--init", "--recursive", "--progress"])

print("Running scons in the godot directory...")
subprocess.run(["scons"], cwd="godot")

print("Generating extension API and interface files...")
subprocess.run(["./bin/godot.linuxbsd.editor.x86_64", "--dump-extension-api", "--dump-gdextension-interface", "--headless"], cwd="godot")

print("Moving generated files to godot-cpp/gdextension/...")
subprocess.run(["mv", "godot/extension_api.json", "godot-cpp/gdextension/extension_api.json"])
subprocess.run(["mv", "godot/gdextension_interface.h", "godot-cpp/gdextension/gdextension_interface.h"])

print("Running scons in the godot-cpp directory...")
subprocess.run(["scons"], cwd="godot-cpp")

print("Done!")