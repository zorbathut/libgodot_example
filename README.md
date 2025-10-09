** What This Is

Example of how to use LibGodot from C++. This is currently aimed at the Godot master branch, since LibGodot hasn't yet been released in a final version; this means it needs to do a whole lot of extra work in order to get a functioning GDExtension with the new APIs.

Once Godot 4.6 is released, most of this won't be necessary.

** How To Run

Run `./runit.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C++, then the label text is updated from the same C++. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

Supports Windows and Linux.

(You can ignore the poetry file, it's just for me.)