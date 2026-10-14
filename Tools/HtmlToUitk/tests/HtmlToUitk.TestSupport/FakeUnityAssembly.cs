using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace HtmlToUitk.TestSupport;

public sealed class FakeUnityAssembly
{
    private readonly List<KeyValuePair<string, string[]>> _enums = new List<KeyValuePair<string, string[]>>();
    private readonly List<string> _strings = new List<string>();
    private readonly string _name;

    public FakeUnityAssembly(string assemblyName)
    {
        _name = assemblyName;
    }

    public FakeUnityAssembly Enum(string fullName, params string[] members)
    {
        _enums.Add(new KeyValuePair<string, string[]>(fullName, members));
        return this;
    }

    public FakeUnityAssembly Strings(params string[] values)
    {
        _strings.AddRange(values);
        return this;
    }

    public byte[] Build()
    {
        var ab = new PersistedAssemblyBuilder(new AssemblyName(_name), typeof(object).Assembly);
        var mb = ab.DefineDynamicModule(_name);
        foreach (var e in _enums)
        {
            var eb = mb.DefineEnum(e.Key, TypeAttributes.Public, typeof(int));
            for (var i = 0; i < e.Value.Length; i++)
            {
                eb.DefineLiteral(e.Value[i], i);
            }

            eb.CreateType();
        }

        var tb = mb.DefineType("Heap.Strings", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var m = tb.DefineMethod("Touch", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        var il = m.GetILGenerator();
        foreach (var s in _strings)
        {
            il.Emit(OpCodes.Ldstr, s);
            il.Emit(OpCodes.Pop);
        }

        il.Emit(OpCodes.Ret);
        tb.CreateType();
        using var ms = new MemoryStream();
        ab.Save(ms);
        return ms.ToArray();
    }

    public string WriteTo(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, Build());
        return path;
    }

    public static FakeUnityAssembly UiElementsSample()
    {
        return new FakeUnityAssembly("UnityEngine.UIElementsModule")
            .Enum("UnityEngine.UIElements.StyleSheets.StylePropertyId", "Unknown", "Custom", "AlignContent", "BorderBottomLeftRadius", "Color", "Flex",
                "JustifyContent", "Margin", "MarginTop", "MarginRight", "MarginBottom", "MarginLeft", "TransitionDuration", "UnityBackgroundScaleMode",
                "UnityFontStyleAndWeight", "UnitySliceTop", "WhiteSpace", "FlexWrap", "FlexGrow", "FlexShrink", "FlexBasis", "Filter", "BackdropFilter",
                "Opacity", "BackgroundRepeat", "Mystery")
            .Enum("UnityEngine.UIElements.Justify", "FlexStart", "Center", "FlexEnd", "SpaceBetween", "SpaceAround", "SpaceEvenly")
            .Enum("UnityEngine.UIElements.Align", "Auto", "FlexStart", "Center", "FlexEnd", "Stretch")
            .Enum("UnityEngine.UIElements.Wrap", "NoWrap", "Wrap", "WrapReverse")
            .Enum("UnityEngine.UIElements.WhiteSpace", "Normal", "NoWrap", "Pre", "PreWrap")
            .Enum("UnityEngine.UIElements.Repeat", "NoRepeat", "Space", "Round", "Repeat")
            .Enum("UnityEngine.UIElements.EasingMode", "Ease", "EaseIn", "EaseOut", "EaseInOut", "Linear", "EaseInSine", "EaseInOutBounce")
            .Enum("UnityEngine.UIElements.FilterFunctionType", "None", "Custom", "Tint", "Opacity", "Blur", "HueRotate", "DropShadow", "Count")
            .Enum("UnityEngine.UIElements.StyleValueFunction", "Unknown", "Var", "Env", "LinearGradient", "NoneFilter", "FilterBlur", "MaterialProperty")
            .Enum("UnityEngine.UIElements.StyleValueKeyword", "Inherit", "Initial", "Auto", "Unset", "None")
            .Enum("UnityEngine.UIElements.PseudoStates", "None", "Active", "Hover", "Checked", "Disabled", "Focus", "Root")
            .Enum("UnityEngine.UIElements.StyleSelectorRelationship", "None", "Child", "Descendent")
            .Enum("UnityEngine.UIElements.StyleSelectorType", "Unknown", "Wildcard", "Type", "Class", "PseudoClass", "ID", "Predicate")
            .Enum("UnityEngine.UIElements.FlexDirection", "Column", "ColumnReverse", "Row", "RowReverse")
            .Strings("align-content", "border-bottom-left-radius", "color", "flex", "justify-content", "margin", "margin-top", "margin-right",
                "margin-bottom", "margin-left", "transition-duration", "-unity-background-scale-mode", "-unity-font-style", "-unity-font-style-and-weight",
                "-unity-slice-top", "background-repeat", "white-space", "flex-wrap", "flex-grow", "flex-shrink", "flex-basis", "filter", "backdrop-filter", "opacity",
                "ease", "ease-in", "ease-out", "ease-in-out", "linear", "ease-in-sine", "ease-in-out-bounce",
                "tint", "blur", "hue-rotate", "drop-shadow", "var", "env", "linear-gradient",
                "inherit", "initial", "auto", "unset", "none", "active", "hover", "checked", "disabled", "focus", "root");
    }
}
