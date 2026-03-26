// C wrapper functions for GDExtension interface calls.
// On WASM, the Mono interpreter can't call native function pointers directly
// (missing interp-to-native trampolines). These wrappers provide stable
// [DllImport]-callable functions that forward to the real GDExtension functions.

#include <stdint.h>

typedef void (*gdext_string_name_new_fn)(void* r_dest, const char* p_contents, uint8_t p_is_static);
typedef void* (*gdext_classdb_get_method_bind_fn)(void* p_classname, void* p_methodname, int64_t p_hash);
typedef void (*gdext_object_method_bind_ptrcall_fn)(void* p_method_bind, void* p_instance, void* p_args, void* p_ret);
typedef uint64_t (*gdext_object_get_instance_id_fn)(void* p_object);
typedef void* (*gdext_get_proc_address_fn)(const char* p_name);

// Stored function pointers
static gdext_get_proc_address_fn s_get_proc_address = 0;
static gdext_string_name_new_fn s_string_name_new = 0;
static gdext_classdb_get_method_bind_fn s_classdb_get_method_bind = 0;
static gdext_object_method_bind_ptrcall_fn s_object_method_bind_ptrcall = 0;
static gdext_object_get_instance_id_fn s_object_get_instance_id = 0;

void gdext_wrapper_set_proc_address(void* proc_address) {
    s_get_proc_address = (gdext_get_proc_address_fn)proc_address;
}

void* gdext_wrapper_get_proc(const char* name) {
    if (!s_get_proc_address) return 0;
    return (void*)s_get_proc_address(name);
}

void gdext_wrapper_load_interface() {
    if (!s_get_proc_address) return;
    s_string_name_new = (gdext_string_name_new_fn)s_get_proc_address("string_name_new_with_latin1_chars");
    s_classdb_get_method_bind = (gdext_classdb_get_method_bind_fn)s_get_proc_address("classdb_get_method_bind");
    s_object_method_bind_ptrcall = (gdext_object_method_bind_ptrcall_fn)s_get_proc_address("object_method_bind_ptrcall");
    s_object_get_instance_id = (gdext_object_get_instance_id_fn)s_get_proc_address("object_get_instance_id");
}

void gdext_wrapper_string_name_new(void* r_dest, const char* p_contents, uint8_t p_is_static) {
    if (s_string_name_new) s_string_name_new(r_dest, p_contents, p_is_static);
}

void* gdext_wrapper_classdb_get_method_bind(void* p_classname, void* p_methodname, int64_t p_hash) {
    if (!s_classdb_get_method_bind) return 0;
    return s_classdb_get_method_bind(p_classname, p_methodname, p_hash);
}

void gdext_wrapper_object_method_bind_ptrcall(void* p_method_bind, void* p_instance, void* p_args, void* p_ret) {
    if (s_object_method_bind_ptrcall) s_object_method_bind_ptrcall(p_method_bind, p_instance, p_args, p_ret);
}

uint64_t gdext_wrapper_object_get_instance_id(void* p_object) {
    if (!s_object_get_instance_id) return 0;
    return s_object_get_instance_id(p_object);
}
