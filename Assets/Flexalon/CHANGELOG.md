# Changelog

## Version 4.4.1

- Unity 6 Compile Fix to Flexalon Random Modifier
- Fix Templates to support new Input System.

## Version 4.4

IMPORTANT: Unity 2019 and 2021 are no longer tested. As always, please back up your project before updating.

### Features

- Added built-in support for the new Input System. It should work out of the box with mouse input. For more advanced usage, see FlexalonInputSystemProvider. All samples have also been updated to work in both new and legacy input systems.
- Breaking Change: Disabled components (MeshRenderer, Image, etc.) are no longer used to determine the size of objects. You can override this behavior with a custom Adapter (see docs).
- Added some helpful methods to FlexalonGridLayout: GetCellPositionWorldSpace and GetCellPositionLocalSpace.
- New FlexalonInteractable property: Set Parent While Dragging. Normally, the interactable is unparented while being dragged. Setting this will set the parent to the currently hovered drag target.
- Support raycasting with Canvas render mode Screen Space Camera.
- Added FlexalonDpiScaler, which scales a Canvas pixel size to match the operating system screen settings.
- Added spacing options to Flexible Layout: Space Evently and Space Around.

### Fixes
- Flexalon no longer preserves aspect ratio for ILayoutElement components. This was never intended.
- Fixed Flexalon Constrant scaling the child of a Canvas.
- Fixed collider adapter not scaling correctly.

## Version 4.3

### Features

- **Improved UGUI Integration**: UGUI components like TextMeshPro and Image will automatically be recomputed when properties change in play mode.
- **FlexalonAspectRatioAdapter**: Set a specific aspect ratio for an object.
- **FlexalonColliderAdapter**: Resizes an attached collider to match the object's layout size.
- **FlexalonObject.UseDefaultAdapter**: new property that can be disabled to make Flexalon treat the object as an empty gameObject and not consider components like MeshRenderer, TextMeshPro, etc.
- **FlexalonInteractable.MaxClickDistance**: Immediately converts a click to a drag if the mouse moves more than the specified distance.

### Fixes

- Support negative Scale values in Flexalon Object.
- Fix floating point error in Flexible Layout preventing wrapping.
- Workaround for a TextMeshPro bug causing hanges in the template scene.
- Display and serialize the "AnimateInWorldSpace" property in Curve Animator.
- Disable the Flexalon Interactable placeholder when dragged off of a drag target.
- Fix Flexible Layout fill not working correctly if there is no remaining space.
- Fix some divide by zero errors.

## Version 4.2

### Features

- New option `Flexalon.SkipInactiveObjects` will cause all inactive gameObjects in the scene to be skipped in layouts. This is the same as enabling FlexalonObject.SkipLayout on individual objects. This option is enabled by default.
- Adding max size to a FlexalonFlexibleLayout can now cause it to wrap.

### Fixes

- Performance: Avoid third layout pass when possible when using Fill size type on children.
- Hotfix 4.2.1: Fix a regression where sometimes child sizes don't update from this performance fix.
- Performance: Make Directions struct immutable.
- Performance: Improve how Flexalon checks for the existance of FlexalonObject.
- Performance: Reduce garbage collection allocations by replacing foreach loops with for loops.
- Interactable: Fix how overlaps are calculated for drag targets with a non-center RectTransform pivot.
- Fix SkipLayout not working correctly for children in a FlexalonGridLayout.
- Fix TextMeshPro adapters not loading in Unity 2022.3 and Unity 6.
- Fix Flexalon adapters not working when a new gameObject is added to a layout in a script before another component is added like a mesh, image, or text.
- Fix how FlexalonConstraint computes fill sizes when the target is scaled.
- Fix max fill size not working on root layouts with RectTransform parents.
- Fix Unity 6 deprecation warnings.

## Version 4.1.2

- Ensure FlexalonObject parameters are read even if the gameObject is inactive.
- Support additive scenes by allowing multiple Flexalon singletons to be in the scene. Only one will be used.
- Use RequireComponent to add FlexalonObject to layouts instead of adding it in OnEnable. This fixes some issues with Undo/Redo.
- Fix a destroyed FlexalonResult being accessed on redo.
- Fix NaN that can happen when assigning zero fill size.
- Fix an exception when leaving prefab mode on some versions of Unity.
- Fix an occasional exception on deserializing a grid layout.
- Fix an error that occurs if a Flexalon Interactables with a UI component is instantiated without a Canvas parent.
- Support canvases inside canvases by treating them as RectTransforms.
- Fix Flexalon Animators on children of a layout freezing when that layout is dragged by a Flexalon Interactable.

## Version 4.1.1

### Fixes

- Avoid Flexalon changing transforms during undo/redo events.
- Avoid updating layout of objects that are being dragged.
- Fix how Flexible Layout gap affects the shrinking of children.
- Correctly serialize FlexalonLerpAnimator._animateInWorldSpace.
- Tweak the FlexalonInteractable insertion algorithm to detect overlaps using the cursor position instead of the object's current animating box position.
- Fix sample ImageAdapter.

## Version 4.1.0

- **Support for Flexalon Copilot!**
- **Support for min and max sizes, and improved Flexible Layout!**

### Features
- Flexalon Object has new fields for min and max. Values can be a fixed number or a percent of the parent.
- All built-in adapters and layouts updated to support min and max.
- Flexible Layout will now shrink objects which have not hit their min size using a similar algorihtm to Flexbox.
- Flexalon Object has new "Skip Layout" field. If set, the gameObject will be skipped by the parent layout.

### Changes
- FlexalonAdapter and FlexalonLayout interface methods now take min and max parameters.
- Flexalon Object no longer auto-computes the offset, rotation, and scale when the transform is edited. This allows more consistent behavior when working with instantiated prefabs.

### Fixes
- Allow pivot and anchor to be set on root objects which are not controlled by a layout or constraint. This allows ScrollRect to position Flexalon layouts properly.
- Fix warnings when user does Undo after a Flexalon component is automatically added.
- Fix exception if TextMeshPro is missing a font asset.
- Fix exception is Image component is sprite asset.

## Version 4.0.0

- **You can now use Flexalon to build UI under a Canvas, animate your layouts, and add click/drag interactions.**

- Coming soon, **Flexalon UI Copilot** will let you use natural language conversation with an AI to build your UI automatically. Learn more at flexalon.com/ai.

- **PLEASE NOTE:** If updating from an older version, this update includes changes that may require you to update your code. As always, save a copy of your assets before updating. See Notable Changes below.

### New UI Features

- Use all Flexalon layouts, constraints, and modifiers to position your content.
- Animate your UI with Lerp and Curve Animators.
- Create click and drag interactions with Flexalon Interactables.
- Bind data to your UI with Flexalon Cloner.
- New UI samples scenes under Flexalon/Samples/Scenes/UI
- See all the details at: flexalon.com/docs/ui

### New Layout Features

- Flexible Layout: New "Gap Type" property lets you space objects evenly to fill the layout.
- Circle Layout: New "Plane" parameter
- Lerp Animator & Curve Animator: New "Animate in World Space" checkbox. When checked (default), animations are based on the global coordinates. When unchecked, animations are based on the object's parent coordinates, which is useful if you're animating both the parent and the child and want them to stay together.

### Notable Changes

- For all of the following changes, your assets will be automatically patched, but you may need to update your code to accommodate new property names, values, or renaming.
- Adapter interface has changed. UpdateSize has been replaced with TryGetScale and TryGetRectSize to support animating rect size and to avoid setting scale and rect size when they are handled by external components.
- Interactable.Collider has been replaced with Interactable.Handle, which supports UI components.
- FlexalonCircleLayout.VerticalAlign has been renamed to FlexalonCircleLayout.PlaneAlign to support the new "Plane" parameter.
- FlexalonCircleLayout.UseWidth has been replaced with FlexalonCircleLayout.InitialRadiusOption to support the new "Plane" parameter.
- FlexalonCircleLayout "In" and "Out" rotations have been flipped. "Backwards" has been renamed to "Backward".
- World space canvas with fill size will no longer scale to fit into the layout. The rect transform size will now be adjusted to fit into the layout.

### Fixes and Changes

- Fixed a bug in nesting Flexible layouts with size type Fill.
- Flexalon Oculus Input Provider, which enables input handling for VR applications, now supports objects with multiple interactables.
- Fixed floating point errors causing Flexible Layout to wrap when it shouldn't.
- Setting Flexalon Object size to fill and also assigning a scale now computes the fill size correctly.
- Added some helper properties to the Directions class.
- GameObjects with Flexalon Constraints can now be placed under a layout - they will be skipped in the layout step.
- Fixed an interactable bug where a drag could start if the mouse is held down and dragged over an object.

---

## Version 3.2.2

- Updated folder organization to support separate layout packages.
- Improved grid layout sample scene.

## Version 3.2.1 (Hotfix)

- Fixed Interactable custom editor not appearing correctly.
- Fixed Flexalon Draggable Max Objects property not working correctly.
- Fixed version checking to show the start screen on update.

## Version 3.2.0 (The XR Update)

Added support for VR interactions with integration for XR Interaction Toolkit and Oculus Interaction SDK.

### NEW FEATURES:
- New 'Flexalon XR Input Provider' allows Flexalon Interactables to be used with interactables from XR Interaction Toolkit.
- New 'Flexalon Oculus Input Provider' allows Flexalon Interactables to be used with interactables from Oculus Interaction SDK.
- New 'Insert Radius' property for Flexalon Interactable allows you to specify how close a dragged object needs to be
  to a layout before it is inserted.

### FIXES AND CHANGES:
- Flexalon Drag Target no longer creates a collider on itself. This prevents it from interfering with physics interactions.
  Instead, it uses a custom overlap detection function with the new Insert Radius property of Flexalon Interactable.
- Flexalon Interactable's generated placeholder no longer has a box collider for the same reason.
- If a Flexalon Interactable is dragged into two drag targets at once, it will now select the one with the nearest child.
- Flexalon Interactable local space restrictions no longer change to world space when leaving a drag target.
  This was causing objects to jump when leaving a drag target in some scenarios. Instead, the interactable continues to use the
  last drag target's local space until a new drag target is detected.
- Fixed a bug in which layouts were not always recomputed correctly when adding children from another layout.
- Fixed a bug in which layouts were not always recomputed correctly when a child without a Flexalon Component was deleted.
- Fixed a bug in which a Flexalon Object's offset would sometimes change unexpectedly.
- The Flexalon Result hidden component will no longer appear when a prefab is selected in the asset browser.
- The Game Object > Flexalon context menu will now add new layouts under the right-clicked gameObject.

## Version 3.1.0

Added new start screen and trial version.

## Version 3.0.0

Another major update with 3D grids, infinite curves, and more!

### NEW FEATURES:
  - Grid Layout:
    - Added a new 'Layers' property allows you to create 3D grids.
    - Added a new 'FlexalonGridCell' component allows you to specify which cell an object should be placed.
    - Added a new 'Cell Size' property allows you to specify a fixed cell size instead of dividing the grid size.
    - Added a new helper functions to retrive children in a grid column/row/layer.
  - Curve Layout: Added new BeforeStart and AfterEnd properties that allow you to create infinite curves:
    - Ping Pong: Extend the curve by continuing in the opposite direction.
    - Extend Line: Extend the curve in a straight line based on the tangent at the start/end of the curve.
    - Repeat: Extend the curve by repeating the curve.
    - Repeat Mirror: Extend the curve by mirroring the curve and repeating it.
 - Interactable:
   - Added a new 'Collider' property to specify a different collider object for click/drag.
   - Added a new 'Margin' property to FlexalonDragTarget which increases the size of the target.
 - FlexalonRigidBodyAnimator now supports RigidBody2D.
 - Scripting API docs are now available at flexalon.com/docs/api

### FIXES AND CHANGES:
 - Interactable Fixes:
    - Local space restrictions and offsets now update which local space is used when dragging between layouts. For example, if you drag a book from horizontal stack of books to a vertical stack of books, the book will rotate to match the hovered stack.
    - Dragging an object no longer centers the object on the mouse.
    - Placeholder size calculation is more accurate.
 - Curve Layout Fixes:
    - Forward, Backward, InWithRoll, and OutWithRoll options now rotate objects correctly when the curve turns upside down.
    - Serialized FlexalonCurveLayout.CurveLength so it is always available.
 - Performance Improvements:
    - Flexalon debug logs are completely compiled out of the build.
    - Improved efficiency of layout editors.
    - Added preprocessor checks to automatically disable features that depend on optional Unity packages.
 - General Fixes:
    - Flexalon layouts no longer apply when disabled.
    - Fixed a bug where adding and removing FlexalonObject component causes weird scaling.
    - Fixed a bug where removing a child from a layout didn't always update the layout.
    - Fixed a bug where editing the transform of a FlexalonConstraint in the editor would update the offset and rotation incorrectly.
    - Fixed the offset for rotated objects with a non-zero component bounds.

## Version 2.0.0

This BIG update to Flexalon includes new layouts, interactions, bug fixes, and more!

### NEW FEATURES
 - Align Layout: Align all children to the parent on the specified axes
 - Shape Layout: Position children in a shape formation with a specified number of sides. Great for crowds and unit formations.
 - Flexalon Interactable: Add click and drag interactions which let users add, remove, and swap objects in layouts.
 - Random Modifier: Add to any layout to randomly modify the positions and rotations of the children.
 - Circle Layout: New 'Radius Type' option can modify the radius for each object or for each iteration around the circle.
 - Curve Layout: New Tangent Mode options can automatically generate smooth or corner tangents. New spacing option 'Evenly Connected' can evenly space curves that have connected start and end. New 'In With Roll' and 'Out With Roll' rotation options.

### BEHAVIOR CHANGES
 - Curve Layout's spacing mode 'Evenly' now places the first object at the start of the curve and the last object at the end of the curve.
 - Curve and Lerp animators now operate in world space, which makes it simpler to transition objects between layouts.
 - TransformUpdater interface now requires a PreUpdate method, which is called before layout starts updating transforms. This can be used to capture the current transform state.
 - A Flexible Layout that does not have Wrap checked will now use the full size of the layout when computing the fill size on both of the non-flex axes. When Wrap is checked the behavior is unchanged: the fill size on the wrap axis depends on the size of each line.
- Improved the behavior of a spiral layout with negative spacing.

### FIXES
 - Flexalon will now update automatically when a new layout component is added.
 - Fixed an issue where Flexalon would sometimes update on recompile.
 - Multi-editing objects previously updated Flexalon once for each object. Now it only happens once.
 - Fixed cases where Flexalon Constraint prevented using the Unity transform control.
 - Fixed the layout size calculation of grid layout with hexagonal cell type.
 - Fixed some cases where the Flexalon Object bounding box visual appears in the wrong place.
 - Fixed NaN errors when a curve layout has two points in the same position.
 - Fixed some instances where Flexalon Result component appears in the inspector (it should always be hidden).
 - Readme asset will only be selected the first time Flexalon is imported.

## Version 1.0.2
  - Spiral 'Use Height' property replaced with flex-like behavior.
  - New 'Spiral Spacing' property adds vertical gaps between spiral objects.
  - Improved ability to modify objects with the standard transform tool.
  - Fixed various Undo / Redo bugs.
  - FlexalonObject's offset, rotation, and scale no longer apply when an object is not in a a layout or constraint.

## Version 1.0.1
  - FlexalonConstraint now supports margins.
  - Add help links to Flexalon website documentation.
  - Hide Documentation from Unity by renaming directory to Documentation~
  - Prevent adding multiple layout components to a GameObject.
  - Prevent setting grid rows or columns to 0.

## Version 1.0.0 - Initial Release!