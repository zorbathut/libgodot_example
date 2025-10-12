** What This Is

Example of how to use LibGodot from C++. This is currently aimed at the Godot master branch, since LibGodot hasn't yet been released in a final version; this means it needs to do a whole lot of extra work in order to get a functioning GDExtension with the new APIs.

Once Godot 4.6 is released, most of this won't be necessary.

** How To Run C++

Run `./runit-cpp.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C++, then the label text is updated from the same C++. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

** How To Run C#

Run `./runit-cs.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C#, then the label text is updated from the same C#. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

Supports Windows and Linux.

(You can ignore the poetry file, it's just for me.)

** License

This is dual-licensed under the MIT License and the Unlicense. You're welcome to treat it as under public domain, if such a thing is legal. Go wild.
