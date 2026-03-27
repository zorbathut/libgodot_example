// main.js - JavaScript glue for driver-cs-web
// This file handles the interface between .NET WASM and the browser

let isRunning = false;
let animationFrameId = null;

// Export function that C# can call to start the frame loop
export function requestAnimationFrame() {
    console.log("[JS] Starting animation frame loop");
    isRunning = true;
    scheduleNextFrame();
}

function scheduleNextFrame() {
    if (!isRunning) {
        return;
    }

    animationFrameId = window.requestAnimationFrame(() => {
        try {
            // Call back into C# to run one frame
            // This will be available as a global function after .NET loads
            if (typeof globalThis.GodotLauncher !== 'undefined' &&
                typeof globalThis.GodotLauncher.RunFrame === 'function') {

                const shouldQuit = globalThis.GodotLauncher.RunFrame();

                if (shouldQuit) {
                    console.log("[JS] Engine requested shutdown");
                    isRunning = false;

                    // Clean up
                    if (typeof globalThis.GodotLauncher.Shutdown === 'function') {
                        globalThis.GodotLauncher.Shutdown();
                    }
                } else {
                    // Schedule next frame
                    scheduleNextFrame();
                }
            } else {
                console.error("[JS] C# RunFrame function not found");
                isRunning = false;
            }
        } catch (error) {
            console.error("[JS] Error in animation frame:", error);
            isRunning = false;
        }
    });
}

// Export function to stop the loop
export function stopAnimationLoop() {
    console.log("[JS] Stopping animation loop");
    isRunning = false;

    if (animationFrameId !== null) {
        window.cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
    }
}

// Initialize when loaded
console.log("[JS] main.js loaded");
