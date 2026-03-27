using System;
using System.Runtime.InteropServices.JavaScript;

public partial class GodotLauncher
{
    private static IntPtr godotInstancePtr = IntPtr.Zero;

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
    public unsafe static int InitializeGodot()
    {
        try
        {
            Console.WriteLine("[C#] InitializeGodot called from JavaScript");

            // Prepare arguments for Godot
            string[] godotArgs = new string[]
            {
                "godot-launcher",
                "--main-pack", "project.pck"  // Point to the preloaded .pck file
            };

            Console.WriteLine("[C#] Creating Godot instance...");

            // Get the function pointer for the callback
            IntPtr callbackPtr = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GDExtensionInitialization*, int>)&LibGodotWeb.InitCallback;
            
            // Create Godot instance via P/Invoke
            godotInstancePtr = LibGodotWeb.libgodot_create_godot_instance(
                godotArgs.Length,
                godotArgs,
                callbackPtr
            );

            if (godotInstancePtr == IntPtr.Zero)
            {
                Console.Error.WriteLine("[C#] Error: Failed to create Godot instance");
                return 1;
            }

            Console.WriteLine("[C#] Godot instance created successfully!");

            // Load GDExtension interface from managed context (not from [UnmanagedCallersOnly])
            LibGodotWeb.LoadGDExtensionInterface();

            // Call start() using our minimal binding
            if (!LibGodotWeb.CallGodotInstanceStart(godotInstancePtr))
            {
                Console.Error.WriteLine("[C#] Error: Failed to start Godot instance");
                LibGodotWeb.libgodot_destroy_godot_instance(godotInstancePtr);
                godotInstancePtr = IntPtr.Zero;
                return 1;
            }

            Console.WriteLine("[C#] Godot start() completed successfully!");
            // Frame loop is started from JavaScript after this returns 0

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
        if (godotInstancePtr == IntPtr.Zero)
        {
            Console.Error.WriteLine("[C#] Error: godotInstancePtr is null in RunFrame");
            return true; // Signal to stop
        }

        try
        {
            // iteration() returns true when the engine wants to quit
            bool shouldQuit = LibGodotWeb.CallGodotInstanceIteration(godotInstancePtr);
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
        }

        Console.WriteLine("[C#] Godot shutdown complete");
    }
}
