#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>

#ifdef __cplusplus
extern "C" {
#endif

float getPixelDensity() {
    // Use pixel density value where the main window is shown
    if ([[NSApplication sharedApplication] mainWindow]) {
        return [[[[NSApplication sharedApplication] mainWindow] screen] backingScaleFactor];
    }

    // Use pixel density value where the first window is shown
    if ([[[NSApplication sharedApplication] windows] count] > 0) {
        return [[[[NSApplication sharedApplication] windows][0] screen] backingScaleFactor];
    }

    // Use pixel density value of the main screen
    if ([NSScreen mainScreen]) {
        return [[NSScreen mainScreen] backingScaleFactor];
    }

    // Use pixel density value of the first screen
    if ([[NSScreen screens] count] > 0) {
        return [[NSScreen screens][0] backingScaleFactor];
    }

    // Assume Retina display
    return 2.0f;
}

#ifdef __cplusplus
}
#endif
