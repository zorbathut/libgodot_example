** How To Run

Run `./runit.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C++, then the label text is updated from the same C++. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

Supports Windows and Linux.

(You can ignore the poetry file, it's just for me.)