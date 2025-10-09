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

        // Create Godot instance via P/Invoke
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

        // Get the GodotInstance object from the native pointer
        // In C# with Godot 4.x, we need to use GodotObject.InstanceFromId or similar
        // However, for libgodot we need to directly work with the instance
        GodotObject? instanceObj = GodotObject.InstanceFromId((ulong)instancePtr);
        if (instanceObj == null)
        {
            Console.Error.WriteLine("Failed to get GodotInstance from pointer");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        GodotInstance? godotInstance = instanceObj as GodotInstance;
        if (godotInstance == null)
        {
            Console.Error.WriteLine("Failed to cast to GodotInstance");
            LibGodot.libgodot_destroy_godot_instance(instancePtr);
            return 1;
        }

        if (!godotInstance.Start())
        {
            Console.Error.WriteLine("Error starting Godot instance");
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

        // Run for 10 seconds, updating the label text each frame
        Stopwatch stopwatch = Stopwatch.StartNew();
        int frameCount = 0;

        while (!godotInstance.Iteration())
        {
            double secondsRemaining = 10.0 - stopwatch.Elapsed.TotalSeconds;

            if (secondsRemaining <= 0)
                break;

            frameCount++;
            targetLabel.Text = $"Frame: {frameCount} - Shutting down in {secondsRemaining:F2} seconds";
        }

        Console.WriteLine("Godot running complete.");

        // Clean up
        LibGodot.libgodot_destroy_godot_instance(instancePtr);
        Console.WriteLine("Godot instance destroyed.");

        return 0;
    }
}
