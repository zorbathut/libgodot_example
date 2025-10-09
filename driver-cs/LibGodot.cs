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
    public static extern IntPtr libgodot_create_godot_instance(
        int p_argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] p_argv,
        GDExtensionInitializationFunction p_init_func);

    [DllImport(LIBGODOT_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libgodot_destroy_godot_instance(IntPtr p_godot_instance);

    // Empty callbacks for GDExtension initialization
    private static void InitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level) { }
    private static void DeinitializeCallback(IntPtr userdata, GDExtensionInitializationLevel level) { }

    // Keep delegates alive to prevent garbage collection
    private static GDExtensionInitializationCallback initDelegate = new GDExtensionInitializationCallback(InitializeCallback);
    private static GDExtensionInitializationCallback deinitDelegate = new GDExtensionInitializationCallback(DeinitializeCallback);

    public static string? RepoPath { get; private set; }

    static LibGodot()
    {
        // Find Git repository root
        for (var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, ".git")))
            {
                RepoPath = dir.FullName;
                break;
            }
        }

        // Set up DLL import resolver for relative paths
        NativeLibrary.SetDllImportResolver(typeof(LibGodot).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, System.Reflection.Assembly asm, DllImportSearchPath? paths)
    {
        if (RepoPath != null && NativeLibrary.TryLoad(System.IO.Path.Combine(RepoPath, libraryName), out var handle))
        {
            return handle;
        }

        // Let the runtime try its defaults (LD_LIBRARY_PATH/rpath/etc.)
        return IntPtr.Zero;
    }

    public static bool InitCallback(IntPtr p_get_proc_address, IntPtr p_library, ref GDExtensionInitialization r_initialization)
    {
        r_initialization.minimum_initialization_level = GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE;
        r_initialization.initialize = Marshal.GetFunctionPointerForDelegate(initDelegate);
        r_initialization.deinitialize = Marshal.GetFunctionPointerForDelegate(deinitDelegate);

        return true;
    }
}
