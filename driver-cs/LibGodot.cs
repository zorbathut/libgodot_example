using System;
using System.Runtime.InteropServices;
using System.Text;

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

// GDExtension interface function delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr GDExtensionInterfaceGetProcAddress(IntPtr p_name);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr GDExtensionInterfaceObjectGetInstanceFromId(ulong p_instance_id);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr GDExtensionInterfaceClassdbGetMethodBind(IntPtr p_classname, IntPtr p_methodname, long p_hash);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void GDExtensionInterfaceObjectMethodBindPtrcall(IntPtr p_method_bind, IntPtr p_instance, IntPtr p_args, IntPtr p_ret);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void GDExtensionInterfaceStringNameNewWithLatin1Chars(IntPtr r_dest, IntPtr p_contents, byte p_is_static);

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

    // GDExtension interface function pointers loaded during initialization
    private static GDExtensionInterfaceObjectGetInstanceFromId? objectGetInstanceFromId;
    private static GDExtensionInterfaceClassdbGetMethodBind? classdbGetMethodBind;
    private static GDExtensionInterfaceObjectMethodBindPtrcall? objectMethodBindPtrcall;
    private static GDExtensionInterfaceStringNameNewWithLatin1Chars? stringNameNewWithLatin1Chars;

    // Cache for the GodotInstance::start() method bind
    private static IntPtr startMethodBind = IntPtr.Zero;

    // StringName size (from godot-cpp)
    private const int STRING_NAME_SIZE = 8;

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

        // Load the GDExtension interface functions we need
        var getProcAddress = Marshal.GetDelegateForFunctionPointer<GDExtensionInterfaceGetProcAddress>(p_get_proc_address);

        // Load object_get_instance_from_id
        IntPtr objectGetInstanceFromIdName = Marshal.StringToHGlobalAnsi("object_get_instance_from_id");
        IntPtr objectGetInstanceFromIdPtr = getProcAddress(objectGetInstanceFromIdName);
        Marshal.FreeHGlobal(objectGetInstanceFromIdName);
        if (objectGetInstanceFromIdPtr != IntPtr.Zero)
        {
            objectGetInstanceFromId = Marshal.GetDelegateForFunctionPointer<GDExtensionInterfaceObjectGetInstanceFromId>(objectGetInstanceFromIdPtr);
        }

        // Load classdb_get_method_bind
        IntPtr classdbGetMethodBindName = Marshal.StringToHGlobalAnsi("classdb_get_method_bind");
        IntPtr classdbGetMethodBindPtr = getProcAddress(classdbGetMethodBindName);
        Marshal.FreeHGlobal(classdbGetMethodBindName);
        if (classdbGetMethodBindPtr != IntPtr.Zero)
        {
            classdbGetMethodBind = Marshal.GetDelegateForFunctionPointer<GDExtensionInterfaceClassdbGetMethodBind>(classdbGetMethodBindPtr);
        }

        // Load object_method_bind_ptrcall
        IntPtr objectMethodBindPtrcallName = Marshal.StringToHGlobalAnsi("object_method_bind_ptrcall");
        IntPtr objectMethodBindPtrcallPtr = getProcAddress(objectMethodBindPtrcallName);
        Marshal.FreeHGlobal(objectMethodBindPtrcallName);
        if (objectMethodBindPtrcallPtr != IntPtr.Zero)
        {
            objectMethodBindPtrcall = Marshal.GetDelegateForFunctionPointer<GDExtensionInterfaceObjectMethodBindPtrcall>(objectMethodBindPtrcallPtr);
        }

        // Load string_name_new_with_latin1_chars
        IntPtr stringNameNewWithLatin1CharsName = Marshal.StringToHGlobalAnsi("string_name_new_with_latin1_chars");
        IntPtr stringNameNewWithLatin1CharsPtr = getProcAddress(stringNameNewWithLatin1CharsName);
        Marshal.FreeHGlobal(stringNameNewWithLatin1CharsName);
        if (stringNameNewWithLatin1CharsPtr != IntPtr.Zero)
        {
            stringNameNewWithLatin1Chars = Marshal.GetDelegateForFunctionPointer<GDExtensionInterfaceStringNameNewWithLatin1Chars>(stringNameNewWithLatin1CharsPtr);
        }

        return true;
    }

    // Minimal binding for GodotInstance::start()
    public static bool CallGodotInstanceStart(ulong instanceId)
    {
        if (objectGetInstanceFromId == null || classdbGetMethodBind == null ||
            objectMethodBindPtrcall == null || stringNameNewWithLatin1Chars == null)
        {
            throw new InvalidOperationException("GDExtension interface functions not loaded");
        }

        // Get the object pointer from the instance ID
        IntPtr objectPtr = objectGetInstanceFromId(instanceId);
        if (objectPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get object instance from ID");
        }

        // Get the method bind for GodotInstance::start() if not already cached
        if (startMethodBind == IntPtr.Zero)
        {
            // Allocate StringName storage (8 bytes each)
            IntPtr classNameStorage = Marshal.AllocHGlobal(STRING_NAME_SIZE);
            IntPtr methodNameStorage = Marshal.AllocHGlobal(STRING_NAME_SIZE);

            try
            {
                // Create StringName objects
                IntPtr classNameStr = Marshal.StringToHGlobalAnsi("GodotInstance");
                IntPtr methodNameStr = Marshal.StringToHGlobalAnsi("start");

                stringNameNewWithLatin1Chars(classNameStorage, classNameStr, 0); // 0 = not static
                stringNameNewWithLatin1Chars(methodNameStorage, methodNameStr, 0);

                Marshal.FreeHGlobal(classNameStr);
                Marshal.FreeHGlobal(methodNameStr);

                // Get the method bind
                startMethodBind = classdbGetMethodBind(classNameStorage, methodNameStorage, 2240911060);

                if (startMethodBind == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to get method bind for GodotInstance::start()");
                }
            }
            finally
            {
                // Clean up StringName storage
                Marshal.FreeHGlobal(classNameStorage);
                Marshal.FreeHGlobal(methodNameStorage);
            }
        }

        // Call the method
        bool returnValue = false;
        IntPtr retPtr = Marshal.AllocHGlobal(Marshal.SizeOf<bool>());
        try
        {
            objectMethodBindPtrcall(startMethodBind, objectPtr, IntPtr.Zero, retPtr);
            returnValue = Marshal.ReadByte(retPtr) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(retPtr);
        }

        return returnValue;
    }
}
