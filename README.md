** How To Run

Run `./runit.py`.

If you don't have scons in your environment and like poetry, `poetry install && poetry run ./runit.py`.

Godot is started from C++, then the label text is updated from the same C++. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

Linux-only right now, sorry. Windows coming soonish.
