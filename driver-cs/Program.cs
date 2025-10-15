using System;
using System.Diagnostics;
using Godot;

namespace DriverCS;

class Program
{
    static int Main(string[] args)
    {
        // set directory to the root
        System.Environment.CurrentDirectory = "/home/zorba/werk/libgodot_example";
        
        Console.WriteLine("Starting Godot instance...");

        // Prepare arguments for Godot
        string[] godotArgs = new string[]
        {
            "driver",
            "--path", "project"
        };

        // Create Godot instance via P/Invoke (without starting)
        IntPtr instancePtr = LibGodot.libgodot_create_godot_instance(
            godotArgs.Length,
            godotArgs,
            LibGodot.InitCallback
        );

        if (instancePtr == IntPtr.Zero)
        {
            Console.Error.WriteLine("Error creating Godot instance");
            return 1;
        }

        Console.WriteLine("Godot instance created successfully!");

        // Call start() using our minimal binding
        if (!LibGodot.CallGodotInstanceStart(instancePtr))
        {
            Console.Error.WriteLine("Error starting Godot instance");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        // Get the GodotInstance object from the native pointer
        GodotInstance? godotInstance = LibGodot.GetGodotInstanceFromPtr(instancePtr);
        if (godotInstance == null)
        {
            Console.Error.WriteLine("Failed to get GodotInstance from pointer");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        Console.WriteLine("Godot started successfully!");

        // Get the SceneTree
        MainLoop? mainLoop = Engine.Singleton.GetMainLoop();
        SceneTree? tree = mainLoop as SceneTree;
        if (tree == null)
        {
            Console.Error.WriteLine("Failed to get SceneTree");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        // Get the current scene
        Node? currentScene = tree.CurrentScene;
        if (currentScene == null)
        {
            Console.Error.WriteLine("No current scene loaded");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        // Find the TargetLabel node
        Label? targetLabel = currentScene.GetNode<Label>("TargetLabel");
        if (targetLabel == null)
        {
            Console.Error.WriteLine("TargetLabel not found in scene");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }
        
        // Find the TIcker node
        // This is here to demonstrate that we can access C# stuff properly
        Ticker? ticker = currentScene.GetNode<Ticker>("Ticker");
        if (ticker == null)
        {
            Console.Error.WriteLine("Ticker not found in scene");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        // Run for 10 seconds, updating the label text each frame
        Stopwatch stopwatch = Stopwatch.StartNew();
        int frameCount = 0;

        while (!godotInstance.Iteration())
        {
            double secondsRemaining = 10.0 - stopwatch.Elapsed.TotalSeconds;

            if (secondsRemaining <= 0)
                break;

            frameCount++;

            string match;
            if (ticker.localAccumulator == frameCount && Ticker.staticAccumulator == frameCount)
            {
                match = "(matches!)";
            }
            else
            {
                match = "(does not match! something is broken)";
            }
            targetLabel.Text = $"Frame: {frameCount} - Ticker values {ticker.localAccumulator} and {Ticker.staticAccumulator} {match} - Shutting down in {secondsRemaining:F2} seconds";
        }

        Console.WriteLine("Godot running complete.");

        // Clean up
        LibGodot.libgodot_destroy_godot_instance(instancePtr);
        Console.WriteLine("Godot instance destroyed.");

        return 0;
    }
}
