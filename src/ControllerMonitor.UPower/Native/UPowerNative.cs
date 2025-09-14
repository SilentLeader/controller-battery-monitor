using System.Runtime.InteropServices;

namespace ControllerMonitor.UPower.Native;

/// <summary>
/// Native P/Invoke bindings for libupower-glib library
/// </summary>
internal static class UPowerNative
{
    private const string LibUPower = "libupower-glib.so.3";
    private const string LibGLib = "libglib-2.0.so.0";
    private const string LibGObject = "libgobject-2.0.so.0";
    
    #region Core UPower Client Functions
    
    /// <summary>
    /// Creates a new UPower client
    /// </summary>
    [DllImport(LibUPower, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_client_new();
    
    /// <summary>
    /// Gets array of devices from UPower client
    /// </summary>
    [DllImport(LibUPower, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_client_get_devices(IntPtr client);
    
    /// <summary>
    /// Sets whether the laptop lid is closed
    /// </summary>
    [DllImport(LibUPower, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void up_client_set_lid_is_closed(IntPtr client, [MarshalAs(UnmanagedType.Bool)] bool is_closed);
    
    /// <summary>
    /// Gets whether the system is on battery power
    /// </summary>
    [DllImport(LibUPower, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool up_client_get_on_battery(IntPtr client);
    
    #endregion
    
    #region Device Property Functions
    
    /// <summary>
    /// Gets device object path
    /// </summary>
    [DllImport(LibUPower, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_device_get_object_path(IntPtr device);
    
    #endregion
    
    #region GLib Object Management
    
    /// <summary>
    /// Increases reference count of GObject
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_object_ref(IntPtr obj);
    
    /// <summary>
    /// Decreases reference count of GObject
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_object_unref(IntPtr obj);
    
    /// <summary>
    /// Gets property from GObject as GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_object_get_property(IntPtr obj, [MarshalAs(UnmanagedType.LPStr)] string property_name, IntPtr value);
    
    #endregion
    
    #region GValue Structure and Functions
    
    /// <summary>
    /// GValue structure for handling GObject property values
    /// Using fixed size to ensure it's unmanaged
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GValue
    {
        public nuint g_type;
        public fixed byte data[16]; // 2 * sizeof(nuint) = 16 bytes on 64-bit
    }
    
    /// <summary>
    /// Initialize a GValue to hold values of the given type
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_value_init(IntPtr value, nuint g_type);
    
    /// <summary>
    /// Clear and reset a GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_value_unset(IntPtr value);
    
    /// <summary>
    /// Get string value from GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_value_get_string(IntPtr value);
    
    /// <summary>
    /// Get double value from GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double g_value_get_double(IntPtr value);
    
    /// <summary>
    /// Get int value from GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int g_value_get_int(IntPtr value);
    
    /// <summary>
    /// Get int64 value from GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long g_value_get_int64(IntPtr value);
    
    /// <summary>
    /// Get boolean value from GValue
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool g_value_get_boolean(IntPtr value);
    
    /// <summary>
    /// Get uint value from GValue  
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint g_value_get_uint(IntPtr value);
    
    /// <summary>
    /// Get uint64 value from GValue  
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong g_value_get_uint64(IntPtr value);
    
    /// <summary>
    /// GType constants for common types - use runtime type retrieval
    /// </summary>
    internal static class GType
    {
        // We need to get the actual GType values at runtime using g_type_from_name
        // These are not compile-time constants in GLib
        
        // Use g_type_from_name to get the actual GType values
        [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
        private static extern nuint g_type_from_name([MarshalAs(UnmanagedType.LPStr)] string name);
        
        // Lazy initialization of type values
        private static nuint? _string;
        private static nuint? _double;
        private static nuint? _int;
        private static nuint? _uint;
        private static nuint? _boolean;
        private static nuint? _int64;
        private static nuint? _uint64;
        
        public static nuint String => _string ??= g_type_from_name("gchararray");
        public static nuint Double => _double ??= g_type_from_name("gdouble");
        public static nuint Int => _int ??= g_type_from_name("gint");
        public static nuint UInt => _uint ??= g_type_from_name("guint");
        public static nuint Boolean => _boolean ??= g_type_from_name("gboolean");
        public static nuint Int64 => _int64 ??= g_type_from_name("gint64");
        public static nuint UInt64 => _uint64 ??= g_type_from_name("guint64");
    }
    
    #endregion
    
    #region GLib Array Functions
    
    /// <summary>
    /// Structure representing GPtrArray as defined in GLib
    /// Note: g_ptr_array_len is a macro, not a function, so we need to access the struct directly
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPtrArray
    {
        public IntPtr pdata;    // gpointer *pdata
        public uint len;        // guint len
    }
    
    /// <summary>
    /// Gets length of GPtrArray by reading the len field directly
    /// This replaces the g_ptr_array_len macro which is not available as a function
    /// </summary>
    internal static uint g_ptr_array_len(IntPtr array)
    {
        if (array == IntPtr.Zero)
            return 0;
            
        unsafe
        {
            GPtrArray* ptrArray = (GPtrArray*)array.ToPointer();
            return ptrArray->len;
        }
    }
    
    /// <summary>
    /// Gets element at index from GPtrArray by calculating pointer offset
    /// This replaces the g_ptr_array_index macro
    /// </summary>
    internal static IntPtr g_ptr_array_index(IntPtr array, uint index)
    {
        if (array == IntPtr.Zero)
            return IntPtr.Zero;
            
        unsafe
        {
            GPtrArray* ptrArray = (GPtrArray*)array.ToPointer();
            if (index >= ptrArray->len)
                return IntPtr.Zero;
                
            IntPtr* dataArray = (IntPtr*)ptrArray->pdata.ToPointer();
            return dataArray[index];
        }
    }
    
    #endregion
    
    #region GLib Signal Functions
    
    /// <summary>
    /// Disconnects signal handler from GObject
    /// </summary>
    [DllImport(LibGObject, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_signal_handler_disconnect(IntPtr instance, ulong handler_id);
    
    #endregion
    
    #region GLib Memory Management
    
    /// <summary>
    /// Frees memory allocated by GLib
    /// </summary>
    [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_free(IntPtr ptr);
    
    /// <summary>
    /// Allocates memory using GLib allocator
    /// </summary>
    [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_malloc(nuint size);
    
    /// <summary>
    /// Decrements the reference count of a GPtrArray and frees it when the reference count reaches zero
    /// </summary>
    [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_ptr_array_unref(IntPtr array);
    
    #endregion
    
    #region GVariant Functions
    
    /// <summary>
    /// Unreferences GVariant
    /// </summary>
    [DllImport(LibGLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_variant_unref(IntPtr variant);
    
    #endregion
    
    #region Helper Methods
        
    /// <summary>
    /// Safely converts IntPtr to UTF-8 string without freeing memory
    /// </summary>
    internal static string? PtrToStringUTF8(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }
    
    /// <summary>
    /// Helper method to allocate and zero-initialize a GValue
    /// </summary>
    private static IntPtr AllocateZeroedGValue()
    {
        unsafe
        {
            IntPtr valuePtr = g_malloc((nuint)sizeof(GValue));
            if (valuePtr == IntPtr.Zero)
                return IntPtr.Zero;
                
            // Zero-initialize the memory - this is critical for GValue
            for (int i = 0; i < sizeof(GValue); i++)
            {
                ((byte*)valuePtr)[i] = 0;
            }
            
            return valuePtr;
        }
    }
    
    /// <summary>
    /// Helper method to safely get a string property from a GObject
    /// </summary>
    internal static string? GetObjectStringProperty(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return null;
            
        try
        {
            g_value_init(valuePtr, GType.String);
            g_object_get_property(obj, propertyName, valuePtr);
            
            IntPtr stringPtr = g_value_get_string(valuePtr);
            return PtrToStringUTF8(stringPtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get a double property from a GObject
    /// </summary>
    internal static double GetObjectDoubleProperty(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return 0.0;
            
        try
        {
            g_value_init(valuePtr, GType.Double);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_double(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get an int property from a GObject
    /// </summary>
    internal static int GetObjectIntProperty(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return 0;
            
        try
        {
            g_value_init(valuePtr, GType.Int);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_int(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get a uint property from a GObject
    /// </summary>
    internal static uint GetObjectUIntProperty(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return 0;
            
        try
        {
            g_value_init(valuePtr, GType.UInt);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_uint(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get an int64 property from a GObject
    /// </summary>
    internal static long GetObjectInt64Property(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return 0L;
            
        try
        {
            g_value_init(valuePtr, GType.Int64);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_int64(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get a boolean property from a GObject
    /// </summary>
    internal static bool GetObjectBooleanProperty(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return false;
            
        try
        {
            g_value_init(valuePtr, GType.Boolean);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_boolean(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    /// <summary>
    /// Helper method to safely get a uint64 property from a GObject
    /// </summary>
    internal static ulong GetObjectUInt64Property(IntPtr obj, string propertyName)
    {
        IntPtr valuePtr = AllocateZeroedGValue();
        if (valuePtr == IntPtr.Zero)
            return 0UL;
            
        try
        {
            g_value_init(valuePtr, GType.UInt64);
            g_object_get_property(obj, propertyName, valuePtr);
            
            return g_value_get_uint64(valuePtr);
        }
        finally
        {
            g_value_unset(valuePtr);
            g_free(valuePtr);
        }
    }
    
    #endregion
}