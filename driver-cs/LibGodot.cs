using System;
using System.Runtime.InteropServices;

namespace DriverCS;

public enum GDExtensionInitializationLevel
{
    GDEXTENSION_INITIALIZATION_CORE,
    GDEXTENSION_INITIALIZATION_SERVERS,
    GDEXTENSION_INITIALIZATION_SCENE,
    GDEXTENSION_INITIALIZATION_EDITOR,
    GDEXTENSION_MAX_INITIALIZATION_LEVEL
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void GDExtensionInitializationCallback(IntPtr userdata, GDExtensionInitializationLevel level);

[StructLayout(LayoutKind.Sequential)]
public struct GDExtensionInitialization
{
    public GDExtensionInitializationLevel minimum_initialization_level;
    public IntPtr userdata;
    public IntPtr initialize;
    public IntPtr deinitialize;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool GDExtensionInitializationFunction(IntPtr p_get_proc_address, IntPtr p_library, ref GDExtensionInitialization r_initialization);

public static class LibGodot
{
    const string LIBGODOT_LIBRARY_NAME = "godot/bin/libgodot.linuxbsd.editor.dev.x86_64.shared_library";

    [DllImport(LIBGODOT_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong libgodot_create_godot_instance(
        int p_argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] p_argv,
        GDExtensionInitializationFunction p_init_func);
    
    [DllImport(LIBGODOT_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong libgodot_create_godot_instance_and_start(
        int p_argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] p_argv,
        GDExtensionInitializationFunction p_init_func);

    [DllImport(LIBGODOT_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libgodot_destroy_godot_instance(ulong p_godot_instance);

    // Empty callbacks for GDExtension initialization
    private static void InitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level) { }
    private static void DeinitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level) { }

    // Keep delegates alive to prevent garbage collection
    private static GDExtensionInitializationCallback initDelegate = new GDExtensionInitializationCallback(InitializeCallback);
    private static GDExtensionInitializationCallback deinitDelegate = new GDExtensionInitializationCallback(DeinitializeCallback);

    public static bool InitCallback(IntPtr p_get_proc_address, IntPtr p_library, ref GDExtensionInitialization r_initialization)
    {
        r_initialization.minimum_initialization_level = GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE;
        r_initialization.initialize = Marshal.GetFunctionPointerForDelegate(initDelegate);
        r_initialization.deinitialize = Marshal.GetFunctionPointerForDelegate(deinitDelegate);

        return true;
    }
}
