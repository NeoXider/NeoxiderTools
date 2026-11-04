using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Optional bridge to the Unity Input System package without a hard assembly reference.
    ///     Every read samples the live current device and its live control; only metadata and getter delegates
    ///     closed over a control are cached, never an input value. Warmed polling of standard controls
    ///     (buttons, keys, Vector2, integer and touch-phase controls) makes no managed allocation on the Mono
    ///     scripting backend, as the Edit and Play Mode allocation tests assert. IL2CPP players are not measured:
    ///     the per-read control lookup goes through <see cref="PropertyInfo.GetValue(object)" />, which Mono serves
    ///     from a cached getter delegate but IL2CPP serves as a reflected invoke.
    ///     A control whose getter cannot be bound (for example a stripped or restricted runtime) falls back to
    ///     reflection, which allocates.
    ///     Without a connected mouse, the mouse position, delta and left button read the current touchscreen's
    ///     primary touch while <see cref="SimulateMouseWithTouches" /> is on, like the legacy Input Manager.
    /// </summary>
    public static class OptionalInputSystemAdapter
    {
        private const int HeldState = 0;
        private const int DownState = 1;
        private const int UpState = 2;
        private const int BoolStateCount = 3;

        private static readonly Type KeyboardType = ResolveType("UnityEngine.InputSystem.Keyboard");
        private static readonly Type MouseType = ResolveType("UnityEngine.InputSystem.Mouse");
        private static readonly Type GamepadType = ResolveType("UnityEngine.InputSystem.Gamepad");
        private static readonly Type TouchscreenType = ResolveType("UnityEngine.InputSystem.Touchscreen");
        private static readonly DeviceAccessor KeyboardDevice = new(KeyboardType);
        private static readonly DeviceAccessor MouseDevice = new(MouseType);
        private static readonly DeviceAccessor GamepadDevice = new(GamepadType);
        private static readonly DeviceAccessor TouchscreenDevice = new(TouchscreenType);
        private static readonly Dictionary<Type, TypeProperties> PropertyCache = new();
        private static readonly Dictionary<Type, MethodInfo> ReadValueMethodCache = new();
        private static readonly Dictionary<Type, TouchPhaseValue[]> TouchPhaseValueCache = new();

        // WHY: bindings hold delegates closed over their control. The table's ephemeron ownership lets a removed
        // device or control be collected without a reset; a strong static dictionary would retain it forever.
        private static ConditionalWeakTable<object, ControlBinding> ControlBindings = new();

        private static readonly string[] BoolStateNames = { "isPressed", "wasPressedThisFrame", "wasReleasedThisFrame" };
        private static readonly string[] TouchPhaseNames = { "Began", "Moved", "Stationary", "Ended", "Canceled" };

        private static readonly TouchPhase[] TouchPhaseTargets =
        {
            TouchPhase.Began, TouchPhase.Moved, TouchPhase.Stationary, TouchPhase.Ended, TouchPhase.Canceled
        };

        private static readonly string[] LetterKeyNames =
        {
            "aKey", "bKey", "cKey", "dKey", "eKey", "fKey", "gKey", "hKey", "iKey", "jKey", "kKey", "lKey", "mKey",
            "nKey", "oKey", "pKey", "qKey", "rKey", "sKey", "tKey", "uKey", "vKey", "wKey", "xKey", "yKey", "zKey"
        };

        private static readonly string[] DigitKeyNames =
        {
            "digit0Key", "digit1Key", "digit2Key", "digit3Key", "digit4Key",
            "digit5Key", "digit6Key", "digit7Key", "digit8Key", "digit9Key"
        };

        private static bool _simulateMouseWithTouches = true;

        /// <summary>
        ///     When on (the default) and no Input System mouse is connected, <see cref="TryGetMousePosition" />,
        ///     <see cref="TryGetMouseDelta" /> and the left button (index 0) of <see cref="TryGetMouseButtonState" />
        ///     read the primary touch of the current touchscreen, matching the legacy
        ///     <c>Input.simulateMouseWithTouches</c> default. A connected mouse always wins; the right and middle
        ///     buttons and the scroll wheel never come from touch. Every <see cref="MouseInputCompat" /> reader
        ///     follows it on the Input System path; legacy and Both projects follow
        ///     <c>Input.simulateMouseWithTouches</c> instead. Restored to on at subsystem registration, so turn it off
        ///     from a <c>RuntimeInitializeLoadType.BeforeSceneLoad</c> hook or later.
        /// </summary>
        public static bool SimulateMouseWithTouches
        {
            get => _simulateMouseWithTouches;
            set => _simulateMouseWithTouches = value;
        }

        /// <summary>
        ///     Drops cached metadata and bindings and restores <see cref="SimulateMouseWithTouches" />. Runs at
        ///     subsystem registration; input values are never cached.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStaticState()
        {
            _simulateMouseWithTouches = true;
            PropertyCache.Clear();
            ReadValueMethodCache.Clear();
            TouchPhaseValueCache.Clear();
            ControlBindings = new ConditionalWeakTable<object, ControlBinding>();
            KeyboardDevice.Invalidate();
            MouseDevice.Invalidate();
            GamepadDevice.Invalidate();
            TouchscreenDevice.Invalidate();
        }

        public static bool IsAvailable => KeyboardType != null || MouseType != null || GamepadType != null ||
                                          TouchscreenType != null;

        public static Vector2 ReadMove()
        {
            float horizontal = 0f;
            float vertical = 0f;

            object keyboard = KeyboardDevice.Read();
            if (keyboard != null)
            {
                if (GetControlBool(keyboard, "aKey", HeldState) ||
                    GetControlBool(keyboard, "leftArrowKey", HeldState))
                {
                    horizontal -= 1f;
                }

                if (GetControlBool(keyboard, "dKey", HeldState) ||
                    GetControlBool(keyboard, "rightArrowKey", HeldState))
                {
                    horizontal += 1f;
                }

                if (GetControlBool(keyboard, "wKey", HeldState) ||
                    GetControlBool(keyboard, "upArrowKey", HeldState))
                {
                    vertical += 1f;
                }

                if (GetControlBool(keyboard, "sKey", HeldState) ||
                    GetControlBool(keyboard, "downArrowKey", HeldState))
                {
                    vertical -= 1f;
                }
            }

            object gamepad = GamepadDevice.Read();
            if (gamepad != null)
            {
                Vector2 leftStick = ReadControlVector2(gamepad, "leftStick");
                horizontal = Mathf.Abs(leftStick.x) > Mathf.Abs(horizontal) ? leftStick.x : horizontal;
                vertical = Mathf.Abs(leftStick.y) > Mathf.Abs(vertical) ? leftStick.y : vertical;
            }

            return new Vector2(horizontal, vertical);
        }

        public static Vector2 ReadLookDelta(float mouseScale)
        {
            Vector2 delta = Vector2.zero;

            object mouse = MouseDevice.Read();
            if (mouse != null)
            {
                delta += ReadControlVector2(mouse, "delta") * mouseScale;
            }

            object gamepad = GamepadDevice.Read();
            if (gamepad != null)
            {
                delta += ReadControlVector2(gamepad, "rightStick");
            }

            return delta;
        }

        public static bool ReadJumpPressed()
        {
            object keyboard = KeyboardDevice.Read();
            bool keyboardJump = keyboard != null && GetControlBool(keyboard, "spaceKey", DownState);

            object gamepad = GamepadDevice.Read();
            bool gamepadJump = gamepad != null && GetControlBool(gamepad, "buttonSouth", DownState);

            return keyboardJump || gamepadJump;
        }

        /// <summary>
        ///     Raw pointer delta for this frame, in pixels. Unlike <see cref="ReadLookDelta" /> the gamepad stick is
        ///     not folded in, so callers that need frame-rate-correct math can treat the two sources separately:
        ///     a pointer delta accumulates per frame, a stick is a continuous rate.
        /// </summary>
        public static Vector2 ReadPointerDelta()
        {
            object mouse = MouseDevice.Read();
            return mouse == null ? Vector2.zero : ReadControlVector2(mouse, "delta");
        }

        /// <summary>
        ///     Gamepad right stick, in the -1..1 range. See <see cref="ReadPointerDelta" /> for why this is separate.
        /// </summary>
        public static Vector2 ReadLookStick()
        {
            object gamepad = GamepadDevice.Read();
            return gamepad == null ? Vector2.zero : ReadControlVector2(gamepad, "rightStick");
        }

        public static bool ReadJumpHeld()
        {
            object keyboard = KeyboardDevice.Read();
            bool keyboardJump = keyboard != null && GetControlBool(keyboard, "spaceKey", HeldState);

            object gamepad = GamepadDevice.Read();
            bool gamepadJump = gamepad != null && GetControlBool(gamepad, "buttonSouth", HeldState);

            return keyboardJump || gamepadJump;
        }

        public static bool ReadRunHeld()
        {
            object keyboard = KeyboardDevice.Read();
            bool keyboardRun = keyboard != null &&
                               (GetControlBool(keyboard, "leftShiftKey", HeldState) ||
                                GetControlBool(keyboard, "rightShiftKey", HeldState));

            object gamepad = GamepadDevice.Read();
            bool gamepadRun = gamepad != null && GetControlBool(gamepad, "leftStickButton", HeldState);

            return keyboardRun || gamepadRun;
        }

        /// <summary>
        ///     Reads <c>isPressed</c>, <c>wasPressedThisFrame</c> or <c>wasReleasedThisFrame</c> of the current
        ///     keyboard's key. Returns whether the key/state exists; <paramref name="state" /> carries its value.
        /// </summary>
        public static bool TryReadKeyState(KeyCode keyCode, string statePropertyName, out bool state)
        {
            state = false;
            object keyboard = KeyboardDevice.Read();
            if (keyboard == null)
            {
                return false;
            }

            string keyPropertyName = GetInputSystemKeyPropertyName(keyCode);
            if (string.IsNullOrEmpty(keyPropertyName))
            {
                return false;
            }

            return TryGetControlBool(keyboard, keyPropertyName, GetBoolStateIndex(statePropertyName), out state);
        }

        /// <summary>
        ///     Pointer position in pixels from the current mouse, or from the primary touch without a mouse (see
        ///     <see cref="SimulateMouseWithTouches" />). Returns false when neither device is available.
        /// </summary>
        public static bool TryGetMousePosition(out Vector3 position)
        {
            object mouse = MouseDevice.Read();
            return TryReadPointerPosition(mouse, SelectTouchFallback(mouse), out position);
        }

        /// <summary>
        ///     Reads <c>isPressed</c>, <c>wasPressedThisFrame</c> or <c>wasReleasedThisFrame</c> of mouse button
        ///     0 (left), 1 (right) or 2 (middle). Without a mouse, button 0 reads the primary touch press (see
        ///     <see cref="SimulateMouseWithTouches" />).
        /// </summary>
        public static bool TryGetMouseButtonState(int buttonIndex, string statePropertyName, out bool state)
        {
            object mouse = MouseDevice.Read();
            object touchscreen = buttonIndex == 0 ? SelectTouchFallback(mouse) : null;
            return TryReadPointerButton(mouse, touchscreen, buttonIndex, statePropertyName, out state);
        }

        /// <summary>
        ///     Pointer movement this frame, in pixels, from the same pointer as <see cref="TryGetMousePosition" />:
        ///     the current mouse, or the primary touch without a mouse. Unlike <see cref="ReadPointerDelta" />, it
        ///     reports whether a pointer exists and follows <see cref="SimulateMouseWithTouches" />.
        /// </summary>
        public static bool TryGetMouseDelta(out Vector2 delta)
        {
            object mouse = MouseDevice.Read();
            return TryReadPointerDelta(mouse, SelectTouchFallback(mouse), out delta);
        }

        /// <summary>
        ///     Mouse wheel movement this frame from the current mouse (<c>Mouse.scroll</c>); y is the vertical wheel.
        ///     With the Input System's default Scroll Delta Behavior (Uniform Across All Platforms) one notch reads
        ///     about 1, like the legacy <c>Input.mouseScrollDelta</c>. Never read from touch.
        /// </summary>
        public static bool TryGetMouseScroll(out Vector2 scroll)
        {
            return TryReadMouseScroll(MouseDevice.Read(), out scroll);
        }

        internal static bool TryReadPointerPosition(object mouse, object touchscreen, out Vector3 position)
        {
            position = Vector3.zero;
            object pointer = mouse ?? touchscreen;
            if (pointer == null)
            {
                return false;
            }

            Vector2 value = ReadControlVector2(pointer, "position");
            position = new Vector3(value.x, value.y, 0f);
            return true;
        }

        internal static bool TryReadPointerButton(object mouse, object touchscreen, int buttonIndex,
            string statePropertyName, out bool state)
        {
            state = false;
            if (mouse == null)
            {
                // WHY: Touchscreen.press mirrors the primary touch contact, so it serves only the left button.
                return buttonIndex == 0 && touchscreen != null &&
                       TryGetControlBool(touchscreen, "press", GetBoolStateIndex(statePropertyName), out state);
            }

            string buttonName = buttonIndex switch
            {
                0 => "leftButton",
                1 => "rightButton",
                2 => "middleButton",
                _ => null
            };

            if (buttonName == null)
            {
                return false;
            }

            return TryGetControlBool(mouse, buttonName, GetBoolStateIndex(statePropertyName), out state);
        }

        internal static bool TryReadPointerDelta(object mouse, object touchscreen, out Vector2 delta)
        {
            delta = Vector2.zero;
            object pointer = mouse ?? touchscreen;
            if (pointer == null)
            {
                return false;
            }

            delta = ReadControlVector2(pointer, "delta");
            return true;
        }

        internal static bool TryReadMouseScroll(object mouse, out Vector2 scroll)
        {
            scroll = Vector2.zero;
            if (mouse == null)
            {
                return false;
            }

            scroll = ReadControlVector2(mouse, "scroll");
            return true;
        }

        /// <summary>
        ///     The touchscreen that stands in for the mouse: the current one when <paramref name="mouse" /> is null
        ///     and <see cref="SimulateMouseWithTouches" /> is on, otherwise null. Every public pointer read selects
        ///     its fallback here.
        /// </summary>
        internal static object SelectTouchFallback(object mouse)
        {
            return mouse == null && _simulateMouseWithTouches ? TouchscreenDevice.Read() : null;
        }

        internal static bool TryGetPrimaryTouch(out int fingerId, out Vector2 position, out TouchPhase phase)
        {
            object touchscreen = TouchscreenDevice.Read();
            object touch = touchscreen == null ? null : GetControl(touchscreen, "primaryTouch");
            return TryReadTouch(touch, out fingerId, out position, out phase);
        }

        internal static bool TryReadTouch(object touch, out int fingerId, out Vector2 position, out TouchPhase phase)
        {
            fingerId = default;
            position = default;
            phase = default;
            if (touch == null)
            {
                return false;
            }

            object phaseControl = GetControl(touch, "phase");
            if (phaseControl == null || !GetBinding(phaseControl).TryReadTouchPhase(phaseControl, out TouchPhase rawPhase))
            {
                return false;
            }

            phase = rawPhase;

            // WHY: raw TouchControl phases persist until the next event, unlike legacy Touch phases.
            if (phase == TouchPhase.Began && !GetControlBool(touch, "press", DownState))
            {
                phase = TouchPhase.Stationary;
            }
            else if ((phase == TouchPhase.Ended || phase == TouchPhase.Canceled) &&
                     !GetControlBool(touch, "press", UpState))
            {
                return false;
            }

            if (!TryReadControlInt32(touch, "touchId", out int touchId) ||
                !TryReadControlVector2(touch, "position", out Vector2 touchPosition))
            {
                return false;
            }

            fingerId = touchId;
            position = touchPosition;
            return true;
        }

        public static string GetInputSystemKeyPropertyName(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                return LetterKeyNames[keyCode - KeyCode.A];
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                return DigitKeyNames[keyCode - KeyCode.Alpha0];
            }

            switch (keyCode)
            {
                case KeyCode.Space: return "spaceKey";
                case KeyCode.Return: return "enterKey";
                case KeyCode.KeypadEnter: return "numpadEnterKey";
                case KeyCode.Escape: return "escapeKey";
                case KeyCode.Tab: return "tabKey";
                case KeyCode.Backspace: return "backspaceKey";
                case KeyCode.LeftShift: return "leftShiftKey";
                case KeyCode.RightShift: return "rightShiftKey";
                case KeyCode.LeftControl: return "leftCtrlKey";
                case KeyCode.RightControl: return "rightCtrlKey";
                case KeyCode.LeftAlt: return "leftAltKey";
                case KeyCode.RightAlt: return "rightAltKey";
                case KeyCode.UpArrow: return "upArrowKey";
                case KeyCode.DownArrow: return "downArrowKey";
                case KeyCode.LeftArrow: return "leftArrowKey";
                case KeyCode.RightArrow: return "rightArrowKey";
                case KeyCode.Insert: return "insertKey";
                case KeyCode.Delete: return "deleteKey";
                case KeyCode.Home: return "homeKey";
                case KeyCode.End: return "endKey";
                case KeyCode.PageUp: return "pageUpKey";
                case KeyCode.PageDown: return "pageDownKey";
                case KeyCode.Numlock: return "numLockKey";
                case KeyCode.CapsLock: return "capsLockKey";
                case KeyCode.F1: return "f1Key";
                case KeyCode.F2: return "f2Key";
                case KeyCode.F3: return "f3Key";
                case KeyCode.F4: return "f4Key";
                case KeyCode.F5: return "f5Key";
                case KeyCode.F6: return "f6Key";
                case KeyCode.F7: return "f7Key";
                case KeyCode.F8: return "f8Key";
                case KeyCode.F9: return "f9Key";
                case KeyCode.F10: return "f10Key";
                case KeyCode.F11: return "f11Key";
                case KeyCode.F12: return "f12Key";
                default: return null;
            }
        }

        private static Type ResolveType(string fullTypeName)
        {
            TryLoadAssembly("Unity.InputSystem");
            TryLoadAssembly("Unity.InputSystem.ForUI");

            Type directType = Type.GetType($"{fullTypeName}, Unity.InputSystem", false);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void TryLoadAssembly(string assemblyName)
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }

        private static int GetBoolStateIndex(string statePropertyName)
        {
            switch (statePropertyName)
            {
                case "isPressed": return HeldState;
                case "wasPressedThisFrame": return DownState;
                case "wasReleasedThisFrame": return UpState;
                default: return -1;
            }
        }

        private static bool GetControlBool(object device, string controlPropertyName, int stateIndex)
        {
            return TryGetControlBool(device, controlPropertyName, stateIndex, out bool state) && state;
        }

        internal static bool TryGetControlBool(object device, string controlPropertyName, string statePropertyName,
            out bool state)
        {
            return TryGetControlBool(device, controlPropertyName, GetBoolStateIndex(statePropertyName), out state);
        }

        private static bool TryGetControlBool(object device, string controlPropertyName, int stateIndex,
            out bool state)
        {
            state = false;
            if (device == null || stateIndex < 0)
            {
                return false;
            }

            object control = GetControl(device, controlPropertyName);
            return control != null && GetBinding(control).TryReadBool(control, stateIndex, out state);
        }

        private static Vector2 ReadControlVector2(object device, string controlPropertyName)
        {
            return TryReadControlVector2(device, controlPropertyName, out Vector2 value) ? value : Vector2.zero;
        }

        private static bool TryReadControlVector2(object owner, string controlPropertyName, out Vector2 value)
        {
            value = default;
            if (owner == null)
            {
                return false;
            }

            object control = GetControl(owner, controlPropertyName);
            return control != null && GetBinding(control).TryReadVector2(control, out value);
        }

        private static bool TryReadControlInt32(object owner, string controlPropertyName, out int value)
        {
            value = default;
            object control = GetControl(owner, controlPropertyName);
            return control != null && GetBinding(control).TryReadInt32(control, out value);
        }

        private static object GetControl(object device, string controlPropertyName)
        {
            PropertyInfo controlProperty = GetInstanceProperty(device.GetType(), controlPropertyName);
            return controlProperty?.GetValue(device);
        }

        private static ControlBinding GetBinding(object control)
        {
            if (!ControlBindings.TryGetValue(control, out ControlBinding binding))
            {
                binding = new ControlBinding();
                ControlBindings.Add(control, binding);
            }

            return binding;
        }

        private static PropertyInfo GetInstanceProperty(Type targetType, string propertyName)
        {
            if (targetType == null || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            if (!PropertyCache.TryGetValue(targetType, out TypeProperties properties))
            {
                properties = new TypeProperties(targetType);
                PropertyCache[targetType] = properties;
            }

            return properties.Get(propertyName);
        }

        private static MethodInfo GetReadValueMethod(Type controlType)
        {
            if (!ReadValueMethodCache.TryGetValue(controlType, out MethodInfo readValueMethod))
            {
                readValueMethod = controlType.GetMethod("ReadValue",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                ReadValueMethodCache[controlType] = readValueMethod;
            }

            return readValueMethod;
        }

        private static TouchPhaseValue[] GetTouchPhaseValues(Type enumType)
        {
            if (TouchPhaseValueCache.TryGetValue(enumType, out TouchPhaseValue[] values))
            {
                return values;
            }

            // WHY: mapping through each defined value's ToString reproduces the reflected name match exactly,
            // including aliases and backends whose enum numbers differ from UnityEngine.TouchPhase.
            List<TouchPhaseValue> mapped = new();
            foreach (object definedValue in Enum.GetValues(enumType))
            {
                int index = Array.IndexOf(TouchPhaseNames, definedValue.ToString());
                if (index >= 0)
                {
                    mapped.Add(new TouchPhaseValue(Convert.ToInt32(definedValue), TouchPhaseTargets[index]));
                }
            }

            values = mapped.ToArray();
            TouchPhaseValueCache[enumType] = values;
            return values;
        }

        private static bool TryParseTouchPhase(string name, out TouchPhase phase)
        {
            phase = default;
            int index = name == null ? -1 : Array.IndexOf(TouchPhaseNames, name);
            if (index < 0)
            {
                return false;
            }

            phase = TouchPhaseTargets[index];
            return true;
        }

        private static TDelegate TryBind<TDelegate>(object target, MethodInfo method) where TDelegate : Delegate
        {
            if (method == null)
            {
                return null;
            }

            try
            {
                return target == null
                    ? Delegate.CreateDelegate(typeof(TDelegate), method, false) as TDelegate
                    : Delegate.CreateDelegate(typeof(TDelegate), target, method, false) as TDelegate;
            }
            catch (ArgumentException)
            {
                // WHY: an unusual member can stay readable through reflection even when binding is rejected.
            }
            catch (MemberAccessException)
            {
                // WHY: restricted runtimes keep the original reflected read.
            }
            catch (NotSupportedException)
            {
                // WHY: the fast path never needs emitted code or runtime generic specialization.
            }

            return null;
        }

        private static T InvokeBound<T>(Func<T> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception exception)
            {
                // WHY: reflection wraps getter faults; bound reads keep the same observable fault boundary.
                throw new TargetInvocationException(exception);
            }
        }

        private readonly struct TouchPhaseValue
        {
            public readonly int Raw;
            public readonly TouchPhase Phase;

            public TouchPhaseValue(int raw, TouchPhase phase)
            {
                Raw = raw;
                Phase = phase;
            }
        }

        /// <summary>
        ///     Public instance properties of one type, including cached misses.
        /// </summary>
        private sealed class TypeProperties
        {
            private readonly Type _type;
            private readonly Dictionary<string, PropertyInfo> _byName = new(StringComparer.Ordinal);

            // WHY: hashing a property name costs more than the whole bound read on Mono. Callers pass interned
            // literals, so an identity lookup answers the warm path; only interned keys are admitted, which keeps
            // runtime-built names from growing it.
            private readonly Dictionary<string, PropertyInfo> _byIdentity = new(IdentityComparer.Instance);

            public TypeProperties(Type type)
            {
                _type = type;
            }

            public PropertyInfo Get(string name)
            {
                if (_byIdentity.TryGetValue(name, out PropertyInfo property))
                {
                    return property;
                }

                if (!_byName.TryGetValue(name, out property))
                {
                    property = _type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    _byName[name] = property;
                }

                if (ReferenceEquals(string.IsInterned(name), name))
                {
                    _byIdentity[name] = property;
                }

                return property;
            }
        }

        private sealed class IdentityComparer : IEqualityComparer<string>
        {
            public static readonly IdentityComparer Instance = new();

            public bool Equals(string x, string y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(string value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        /// <summary>
        ///     Lazily bound static <c>current</c> accessor of one optional device type.
        /// </summary>
        private sealed class DeviceAccessor
        {
            private readonly Type _deviceType;
            private bool _resolved;
            private PropertyInfo _currentProperty;
            private Func<object> _current;

            public DeviceAccessor(Type deviceType)
            {
                _deviceType = deviceType;
            }

            public void Invalidate()
            {
                _resolved = false;
                _currentProperty = null;
                _current = null;
            }

            public object Read()
            {
                if (_deviceType == null)
                {
                    return null;
                }

                if (!_resolved)
                {
                    _currentProperty = _deviceType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                    MethodInfo getter = _currentProperty?.GetGetMethod(true);
                    _current = getter == null || getter.ReturnType.IsValueType ? null : TryBind<Func<object>>(null, getter);
                    _resolved = true;
                }

                if (_current != null)
                {
                    return InvokeBound(_current);
                }

                return _currentProperty?.GetValue(null);
            }
        }

        /// <summary>
        ///     Per-control getter bindings. Each read invokes the live getter again: Input System can update more
        ///     than once in a Unity frame, so no value, edge or frame number is cached here.
        /// </summary>
        private sealed class ControlBinding
        {
            private readonly PropertyInfo[] _boolProperties = new PropertyInfo[BoolStateCount];
            private readonly Func<bool>[] _boolGetters = new Func<bool>[BoolStateCount];
            private int _resolvedBoolStates;
            private bool _valueResolved;
            private MethodInfo _readValue;
            private Func<Vector2> _readVector2;
            private Func<int> _readInt32;
            private Func<int> _readEnum;
            private TouchPhaseValue[] _enumTouchPhases;

            public bool TryReadBool(object control, int stateIndex, out bool state)
            {
                int mask = 1 << stateIndex;
                if ((_resolvedBoolStates & mask) == 0)
                {
                    PropertyInfo property = GetInstanceProperty(control.GetType(), BoolStateNames[stateIndex]);
                    if (property?.PropertyType == typeof(bool))
                    {
                        _boolProperties[stateIndex] = property;
                        _boolGetters[stateIndex] = TryBind<Func<bool>>(control, property.GetGetMethod(true));
                    }

                    _resolvedBoolStates |= mask;
                }

                PropertyInfo stateProperty = _boolProperties[stateIndex];
                if (stateProperty == null)
                {
                    state = false;
                    return false;
                }

                Func<bool> getter = _boolGetters[stateIndex];
                state = getter != null ? InvokeBound(getter) : (bool)stateProperty.GetValue(control);
                return true;
            }

            public bool TryReadVector2(object control, out Vector2 value)
            {
                ResolveValue(control);
                if (_readVector2 != null)
                {
                    value = InvokeBound(_readVector2);
                    return true;
                }

                if (ReadBoxed(control) is Vector2 vector)
                {
                    value = vector;
                    return true;
                }

                value = default;
                return false;
            }

            public bool TryReadInt32(object control, out int value)
            {
                ResolveValue(control);
                if (_readInt32 != null)
                {
                    value = InvokeBound(_readInt32);
                    return true;
                }

                if (ReadBoxed(control) is int number)
                {
                    value = number;
                    return true;
                }

                value = default;
                return false;
            }

            public bool TryReadTouchPhase(object control, out TouchPhase phase)
            {
                ResolveValue(control);
                if (_readEnum == null)
                {
                    return TryParseTouchPhase(ReadBoxed(control)?.ToString(), out phase);
                }

                int raw = InvokeBound(_readEnum);
                for (int i = 0; i < _enumTouchPhases.Length; i++)
                {
                    if (_enumTouchPhases[i].Raw == raw)
                    {
                        phase = _enumTouchPhases[i].Phase;
                        return true;
                    }
                }

                phase = default;
                return false;
            }

            private void ResolveValue(object control)
            {
                if (_valueResolved)
                {
                    return;
                }

                _readValue = GetReadValueMethod(control.GetType());
                Type valueType = _readValue?.ReturnType;
                if (valueType == typeof(Vector2))
                {
                    _readVector2 = TryBind<Func<Vector2>>(control, _readValue);
                }
                else if (valueType == typeof(int))
                {
                    _readInt32 = TryBind<Func<int>>(control, _readValue);
                }
                else if (valueType != null && valueType.IsEnum && Enum.GetUnderlyingType(valueType) == typeof(int))
                {
                    // WHY: binding an Int32-based enum getter to Func<int> keeps the value unboxed; the reflected
                    // ToString path is kept for every other enum shape.
                    _readEnum = TryBind<Func<int>>(control, _readValue);
                    _enumTouchPhases = _readEnum == null ? null : GetTouchPhaseValues(valueType);
                }

                _valueResolved = true;
            }

            private object ReadBoxed(object control)
            {
                return _readValue?.Invoke(control, null);
            }
        }
    }
}
