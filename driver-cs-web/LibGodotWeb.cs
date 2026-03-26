using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public enum GDExtensionInitializationLevel
{
    GDEXTENSION_INITIALIZATION_CORE,
    GDEXTENSION_INITIALIZATION_SERVERS,
    GDEXTENSION_INITIALIZATION_SCENE,
    GDEXTENSION_INITIALIZATION_EDITOR,
    GDEXTENSION_MAX_INITIALIZATION_LEVEL
}

[StructLayout(LayoutKind.Sequential)]
public struct GDExtensionInitialization
{
    public GDExtensionInitializationLevel minimum_initialization_level;
    public IntPtr userdata;
    public IntPtr initialize;
    public IntPtr deinitialize;
}

public static unsafe class LibGodotWeb
{
    // Library name for P/Invoke — must match NativeFileReference filename for Godot functions
    private const string LIBGODOT = "libgodot.web.template_release.wasm32.nothreads.cs_webexport";
    // C wrappers — use same lib name since everything is linked into one WASM module
    private const string WRAPPERS = LIBGODOT;

    // --- Godot libgodot API ---
    [DllImport(LIBGODOT, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr libgodot_create_godot_instance(
        int p_argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPUTF8Str)] string[] p_argv,
        IntPtr p_init_func);

    [DllImport(LIBGODOT, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libgodot_destroy_godot_instance(IntPtr p_godot_instance);

    // --- C wrapper functions for GDExtension calls (avoids WASM trampoline issues) ---
    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern void gdext_wrapper_set_proc_address(IntPtr proc_address);

    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern void gdext_wrapper_load_interface();

    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern void gdext_wrapper_string_name_new(IntPtr r_dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string p_contents, byte p_is_static);

    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr gdext_wrapper_classdb_get_method_bind(IntPtr p_classname, IntPtr p_methodname, long p_hash);

    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern void gdext_wrapper_object_method_bind_ptrcall(IntPtr p_method_bind, IntPtr p_instance, IntPtr p_args, IntPtr p_ret);

    [DllImport(WRAPPERS, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong gdext_wrapper_object_get_instance_id(IntPtr p_object);

    // Cache for the GodotInstance::start() method bind
    private static IntPtr startMethodBind = IntPtr.Zero;

    // StringName size
    private const int STRING_NAME_SIZE = 8;

    // Empty callbacks for GDExtension initialization
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void InitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level)
    {
        Console.WriteLine($"[C# InitializeCallback] level={level}");
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void DeinitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level)
    {
        Console.WriteLine($"[C# DeinitializeCallback] level={level}");
    }

    // Saved from InitCallback for later use
    private static IntPtr savedGetProcAddress;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int InitCallback(IntPtr p_get_proc_address, IntPtr p_library, GDExtensionInitialization* r_initialization)
    {
        // Keep this callback absolutely minimal — any complex operations
        // (DllImport calls, function pointer address-of, etc.) crash the WASM interpreter.
        r_initialization->minimum_initialization_level = GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE;
        r_initialization->userdata = IntPtr.Zero;
        r_initialization->initialize = IntPtr.Zero;
        r_initialization->deinitialize = IntPtr.Zero;
        savedGetProcAddress = p_get_proc_address;
        return 1;
    }

    /// <summary>
    /// Load GDExtension interface and bind GodotInstance::start().
    /// Call this after libgodot_create_godot_instance returns.
    /// </summary>
    public static void LoadGDExtensionInterface()
    {
        Console.WriteLine("[C#] Loading GDExtension interface via C wrappers...");

        gdext_wrapper_set_proc_address(savedGetProcAddress);
        gdext_wrapper_load_interface();

        // Create StringNames and get the method bind for GodotInstance::start()
        IntPtr classNameStorage = Marshal.AllocHGlobal(STRING_NAME_SIZE);
        IntPtr methodNameStorage = Marshal.AllocHGlobal(STRING_NAME_SIZE);

        try
        {
            gdext_wrapper_string_name_new(classNameStorage, "GodotInstance", 0);
            gdext_wrapper_string_name_new(methodNameStorage, "start", 0);

            startMethodBind = gdext_wrapper_classdb_get_method_bind(classNameStorage, methodNameStorage, 2240911060);
            Console.WriteLine($"[C#] GodotInstance::start() method bind: {startMethodBind}");
        }
        finally
        {
            Marshal.FreeHGlobal(classNameStorage);
            Marshal.FreeHGlobal(methodNameStorage);
        }

        Console.WriteLine("[C#] GDExtension interface loaded successfully");
    }

    public static bool CallGodotInstanceStart(IntPtr godotInstancePtr)
    {
        if (startMethodBind == IntPtr.Zero)
            throw new InvalidOperationException("GodotInstance::start() method bind not initialized");

        IntPtr retPtr = Marshal.AllocHGlobal(sizeof(byte));
        try
        {
            gdext_wrapper_object_method_bind_ptrcall(startMethodBind, godotInstancePtr, IntPtr.Zero, retPtr);
            return Marshal.ReadByte(retPtr) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(retPtr);
        }
    }

    public static Godot.GodotInstance? GetGodotInstanceFromPtr(IntPtr godotInstancePtr)
    {
        ulong instanceId = gdext_wrapper_object_get_instance_id(godotInstancePtr);
        if (instanceId == 0)
            return null;

        Godot.GodotObject? obj = Godot.GodotObject.InstanceFromId(instanceId);
        return obj as Godot.GodotInstance;
    }
}
