#include <iostream>
#include <vector>
#include <string>
#include "../godot/core/extension/libgodot.h"
#include "../godot-cpp/gdextension/gdextension_interface.h"

// Empty callbacks for GDExtension initialization
static void initialize_callback(void* userdata, GDExtensionInitializationLevel level) {
    // Empty - no extension initialization needed
}

static void deinitialize_callback(void* userdata, GDExtensionInitializationLevel level) {
    // Empty - no extension cleanup needed
}

// GDExtension initialization function
static GDExtensionBool init_callback(GDExtensionInterfaceGetProcAddress p_get_proc_address, 
                                    GDExtensionClassLibraryPtr p_library, 
                                    GDExtensionInitialization* r_initialization) {
    r_initialization->minimum_initialization_level = GDEXTENSION_INITIALIZATION_CORE;
    r_initialization->userdata = nullptr;
    r_initialization->initialize = initialize_callback;
    r_initialization->deinitialize = deinitialize_callback;
    return true;
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
    
    // Clean up
    libgodot_destroy_godot_instance(instance);
    std::cout << "Godot instance destroyed." << std::endl;
    
    return 0;
}