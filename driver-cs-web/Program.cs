using System;
using System.Runtime.InteropServices.JavaScript;
using Godot;

namespace DriverCSWeb;

public partial class Program
{
    private static IntPtr godotInstancePtr = IntPtr.Zero;
    private static GodotInstance? godotInstance = null;

    public static void Main(string[] args)
    {
        Console.WriteLine("[C#] Driver-cs-web started");

        // The actual initialization will be triggered by JavaScript calling InitializeGodot
    }

    /// <summary>
    /// Called from JavaScript to initialize Godot.
    /// This is the entry point that JavaScript will invoke after .NET WASM loads.
    /// </summary>
    [JSExport]
    public static int InitializeGodot()
    {
        try
        {
            Console.WriteLine("[C#] InitializeGodot called from JavaScript");

            // Prepare arguments for Godot
            string[] godotArgs = new string[]
            {
                "driver-web",
                "--path", "/project"  // In WASM, we'll need to mount the project files
            };

            Console.WriteLine("[C#] Creating Godot instance...");

            // Create Godot instance via P/Invoke
            godotInstancePtr = LibGodotWeb.libgodot_create_godot_instance(
                godotArgs.Length,
                godotArgs,
                LibGodotWeb.InitCallback
            );

            if (godotInstancePtr == IntPtr.Zero)
            {
                Console.Error.WriteLine("[C#] Error: Failed to create Godot instance");
                return 1;
            }

            Console.WriteLine("[C#] Godot instance created successfully!");

            // Call start() using our minimal binding
            if (!LibGodotWeb.CallGodotInstanceStart(godotInstancePtr))
            {
                Console.Error.WriteLine("[C#] Error: Failed to start Godot instance");
                LibGodotWeb.libgodot_destroy_godot_instance(godotInstancePtr);
                godotInstancePtr = IntPtr.Zero;
                return 1;
            }

            // Get the GodotInstance object from the native pointer
            godotInstance = LibGodotWeb.GetGodotInstanceFromPtr(godotInstancePtr);
            if (godotInstance == null)
            {
                Console.Error.WriteLine("[C#] Error: Failed to get GodotInstance from pointer");
                LibGodotWeb.libgodot_destroy_godot_instance(godotInstancePtr);
                godotInstancePtr = IntPtr.Zero;
                return 1;
            }

            Console.WriteLine("[C#] Godot started successfully!");

            // Request the first frame
            JSHost.ImportAsync("requestAnimationFrame", "./main.js")
                .ContinueWith(_ =>
                {
                    Console.WriteLine("[C#] Starting animation frame loop");
                });

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[C#] Exception in InitializeGodot: {ex}");
            return 1;
        }
    }

    /// <summary>
    /// Called each frame from JavaScript via requestAnimationFrame.
    /// Returns true if the engine wants to quit.
    /// </summary>
    [JSExport]
    public static bool RunFrame()
    {
        if (godotInstance == null)
        {
            Console.Error.WriteLine("[C#] Error: godotInstance is null in RunFrame");
            return true; // Signal to stop
        }

        try
        {
            // iteration() returns true when the engine wants to quit
            bool shouldQuit = godotInstance.Iteration();
            return shouldQuit;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[C#] Exception in RunFrame: {ex}");
            return true; // Signal to stop on error
        }
    }

    /// <summary>
    /// Called from JavaScript when shutting down
    /// </summary>
    [JSExport]
    public static void Shutdown()
    {
        Console.WriteLine("[C#] Shutting down Godot...");

        if (godotInstancePtr != IntPtr.Zero)
        {
            LibGodotWeb.libgodot_destroy_godot_instance(godotInstancePtr);
            godotInstancePtr = IntPtr.Zero;
            godotInstance = null;
        }

        Console.WriteLine("[C#] Godot shutdown complete");
    }
}
