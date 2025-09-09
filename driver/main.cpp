#include <iostream>
#include <vector>
#include <string>
#include "../godot-cpp/gdextension/gdextension_interface.h"
#include "../godot-cpp/gen/include/godot_cpp/classes/godot_instance.hpp"
#include "../godot-cpp/include/godot_cpp/core/object.hpp"
#include "../godot-cpp/include/godot_cpp/godot.hpp"

// Forward declarations to avoid including godot headers
typedef void *CallbackData;
typedef void *ExecutorData;
typedef void (*InvokeCallback)(CallbackData p_data);
typedef void (*InvokeCallbackFunction)(InvokeCallback p_callback, CallbackData p_callback_data, ExecutorData p_executor_data);

extern "C" {
    GDExtensionObjectPtr libgodot_create_godot_instance(int p_argc, char *p_argv[], GDExtensionInitializationFunction p_init_func, InvokeCallbackFunction p_async_func, ExecutorData p_async_data, InvokeCallbackFunction p_sync_func, ExecutorData p_sync_data);
    void libgodot_destroy_godot_instance(GDExtensionObjectPtr p_godot_instance);
}

// Empty callbacks for GDExtension initialization
static void initialize_callback(godot::ModuleInitializationLevel level) {
    // Empty - no extension initialization needed
}

static void deinitialize_callback(godot::ModuleInitializationLevel level) {
    // Empty - no extension cleanup needed
}

// GDExtension initialization function
static GDExtensionBool init_callback(GDExtensionInterfaceGetProcAddress p_get_proc_address, 
                                    GDExtensionClassLibraryPtr p_library, 
                                    GDExtensionInitialization* r_initialization) {
    godot::GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);
    init_obj.register_initializer(initialize_callback);
    init_obj.register_terminator(deinitialize_callback);
    init_obj.set_minimum_library_initialization_level(godot::MODULE_INITIALIZATION_LEVEL_CORE);
    return init_obj.init();
}

int main() {
    std::cout << "Starting Godot instance..." << std::endl;
    
    // Prepare arguments for Godot
    std::vector<std::string> arg_strings = {
        "driver",
        "--headless"
    };
    
    // Convert to char* array
    std::vector<char*> args;
    for (auto& arg : arg_strings) {
        args.push_back(const_cast<char*>(arg.c_str()));
    }
    
    // Create Godot instance
    GDExtensionObjectPtr instance = libgodot_create_godot_instance(
        args.size(),
        args.data(),
        init_callback,
        nullptr,  // async func
        nullptr,  // async data
        nullptr,  // sync func
        nullptr   // sync data
    );
    
    if (!instance) {
        std::cerr << "Error creating Godot instance" << std::endl;
        return 1;
    }
    
    std::cout << "Godot instance created successfully!" << std::endl;
    
    // Convert GDExtensionObjectPtr to godot::GodotInstance using godot-cpp
    godot::Object* obj = godot::internal::get_object_instance_binding(instance);
    godot::GodotInstance* godot_instance = static_cast<godot::GodotInstance*>(obj);
    
    if (!godot_instance) {
        std::cerr << "Failed to get GodotInstance from GDExtensionObjectPtr" << std::endl;
        libgodot_destroy_godot_instance(instance);
        return 1;
    }
    
    if (!godot_instance->start()) {
        std::cerr << "Error starting Godot instance" << std::endl;
        libgodot_destroy_godot_instance(instance);
        return 1;
    }
    
    std::cout << "Godot started successfully!" << std::endl;
    
    // Run a few iterations to let Godot initialize
    for (int i = 0; i < 10 && !godot_instance->iteration(); i++) {
        std::cout << "Iteration " << (i + 1) << std::endl;
    }
    
    std::cout << "Godot running complete." << std::endl;
    
    // Clean up
    libgodot_destroy_godot_instance(instance);
    std::cout << "Godot instance destroyed." << std::endl;
    
    return 0;
}