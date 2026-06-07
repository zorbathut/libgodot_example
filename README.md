## Intro

LibGodot is a framework designed to make Godot embeddable inside other programs. Yay! Examples of things you can do with this:

* Embed Godot within a C# program that uses NUnit/xUnit for completely native testing
* Embed Godot within Unreal Engine so you can open a Godot engine natively in the same process space and develop while you develop
* Embed Godot within something with a nasty bootstrap process so you can take care of that yourself without needing to build it into Godot
* Probably other things!

The current system supports opening a single Godot and then shutting it down, once. It does not support repeated open/shutdowns, nor does it support parallel independent Godots. It does not yet support running Godot's UI within an application-controlled window; Godot either makes its own window or runs in Headless mode and has no graphics output.

Godot doesn't currently provide official builds of the linkable Godot libraries, so you get to compile it yourself.

### I'd like to dynamically link Godot from C++ on Windows or Linux, how can I do that?

Check out this repository!

Note that as of this writing, `godot-cpp` has not been updated to 4.6, so you get to build godot-cpp also. This is an extra pain because in order to build godot-cpp, you need to build Godot itself in executable form, which means you have to do a full build of Godot, *twice*, in order to get this working.

### . . . on Mac?

I don't have a Mac and can't test that. Sorry. I've heard this is a little finicky. Pull requests accepted here, and if it requires Godot engine requests, go send a pull request to the Godot team.

### . . . from Python or Rust or Malbolge?

I'm not going to write an example for every language, but the tl;dr is that all you really need is a Godot API implementation like `godot-cpp` and then just do the same things that you see in [driver-cpp-shared/main.cpp](https://github.com/zorbathut/libgodot_example/blob/master/driver-cpp-shared/main.cpp).

### . . . without messing with godot-cpp's `internal` functionality?

The current design requires that interface layers either provide their own custom code to wrap the libgodot calls, or that you do stuff that no other API requires. I was unable to convince people to change this design. So, figure out how to hack your Godot library layer to do the things you need. godot-cpp is convenient here because it unintentionally exposes the exact thing we need as part of an "internal-only" interface. Not internal anymore!

### . . . from C#?

Haha, man.

Yeah I tried to get this one working also.

You can run Godot from C# *as long as Godot does not actually use C# itself*. Obviously "C# Godot from C#" is valuable, right? But this takes engine changes. Conveniently, you're building Godot anyway for this, so go check out [this branch](https://github.com/zorbathut/libgodot_example/tree/csharp).

Perhaps someday the GDExtension C# layer will be ready and this can be better handled.

### . . . statically-linked?

[Doesn't work, sorry](https://github.com/godotengine/godot/issues/111876).

Yes, I know the build system claims it does. It's lying.

### . . . on Android, iPhone, Nintendo Switch, Stadia, Apple Pippin, Nokia N-Gage . . .

Right now, only supported on Linux, Windows, and supposedly Mac desktop. But it honestly wouldn't be hard to add support for other platforms; I encourage you to do so and upstream it.

## How To Run

Run `./runit-cpp-shared.py`.

It will take a while; it has to build the entire Godot engine twice, plus some more stuff.

Godot is started from C++, then the label text is updated from the same C++. There's no scripting in this project! It's all driven by the outer harness starting Godot itself.

Supports Windows and Linux.

(You can ignore the poetry file, it's just for me.)

## How To Embed In Your Own Project

This requires building a custom Godot to work so it's a bit of a pain to work into existing projects. I somewhat-maintain [an example framework for vendoring Godot](https://github.com/zorbathut/dieselhorse_godot_framework) and I recommend at least using it as a reference; it's designed to make it as easy as possible to build and it handles a *lot* of weird corner cases and annoyances. If you don't at least reference it, you're going to spend a lot of time stubbing your toes on the same issues I did. My toes are already stubbed; take advantage of that fact.

## This whole thing seems . . . kind of half-baked?

No comment.

## I'm having an issue, can I talk to you?

You can file an issue if you like. Alternatively, Discord name is the same as Github name, feel free to ping me. Note that most questions will probably be answered by "go check libgodot_example, it does that", "go check dieselhorse_godot_framework, it does that", "I don't have an example for that, sorry", or "that doesn't work, sorry". But you're still welcome to ask.

I do accept contract work though right now I charge, like, way more than you should probably want to pay. But I'm also really bad at managing my time so I'm probably more welcome to help you out for free than you might think :V

## So, hey, wasn't the issue with C# web export some gnarly problem where the .net runtime and Godot runtime both expected to be in charge and running the whole thing? Could you maybe use this to run Godot within the .net runtime, and get a fully working C# web export?

Maybe. I think it's actually promising.

I've got a hacky attempt [over here](https://github.com/zorbathut/libgodot_example/tree/webasm-attempt).  It's kinda old and I never got it working properly.

One issue you'll run into is that the official webasm .net runtimes *require* specific versions of webasm, and at least before .net 10, the version they required was so old that Godot didn't support it. Nothing stops you from just bypassing the Godot checks, of course, but I assume there was a good reason for this version restriction.

Is .net 10 better? Dunno! Haven't checked. Go check that.

The issue I ran into with my above attempt is that it *seemed* like everything was working properly, but then Godot couldn't access the virtual filesystem. Or, more precisely, it would attempt to access the virtual filesystem, verify that the files it wanted existed in it, then go to open the files - namely, the project - and fail with file-not-found errors. I suspect I could have solved this if I'd kept working at it but I'd already invested more time than I was willing to.

I think it's quite likely that this would work great, and I'd happily talk to people on it, but I don't have a lot of actual development time to spend.

## License

The original code in this repository is dual-licensed under MIT or the [Unlicense](http://unlicense.org); take your pick. Godot and the third-party libraries bundled as submodules (`godot`, `godot-cpp`) are provided under their own licenses.
