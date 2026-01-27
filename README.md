## What This Is

This is the C# branch of the Libgodot Example. If you haven't read the readme [on the main branch](https://github.com/zorbathut/libgodot_example) you should start with that.

## How To Run C#

Run `./runit-cs.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C#, then the label text is updated from the same C#. There *is* scripting in this project; there's a script that counts frames, both as a local and as a static, and the label text is updated *based on the data in that count*. This shows that you can access within-Godot objects from the outside. While this example doesn't call functions on those objects, that works too.

## How To Use In Your Own Project

Note that this relies on a number of small patches to Godot, which are included in the code that it builds. You can find them with Git; I'm not going to give you a commandline because you will *need* to understand Git reasonably well to get this working on your own project.

I somewhat-maintain [an example framework for vendoring Godot](github.com/zorbathut/dieselhorse_godot_framework) and I recommend at least using it as a reference; it's designed to make it as easy as possible to build and it handles a *lot* of weird corner cases and annoyances. If you don't at least reference it, you're going to spend a lot of time stubbing your toes on the same issues I did. My toes are already stubbed; take advantage of that fact.

## License

This is dual-licensed under the MIT License and the Unlicense. You're welcome to treat it as under public domain, if such a thing is legal. Go wild.
