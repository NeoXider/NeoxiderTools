using System;

namespace Neo.Demo.Editor
{
    /// <summary>Which UPM sample folder a builder's scene path is relative to.</summary>
    public enum DemoSample
    {
        /// <summary>"Demo Scenes" sample: Assets/Neoxider/Samples/Demo or Assets/Samples/NeoxiderTools/&lt;version&gt;/Demo Scenes.</summary>
        Demo = 0,

        /// <summary>"NeoxiderPages" sample next to the Demo sample.</summary>
        NeoxiderPages = 1
    }

    /// <summary>
    ///     Registers a static method as the builder of one demo scene. The method is either
    ///     <c>static void Build(DemoSceneBuildContext ctx)</c> or parameterless, lives in the Neo.Demo.Editor
    ///     assembly (or any editor assembly referencing it) and authors the scene's generated roots through
    ///     <see cref="DemoSceneBuildContext" /> and <see cref="DemoSceneKit" />. The runner opens the scene,
    ///     runs every builder registered for it (by <see cref="Order" />), prunes stale generated objects and saves.
    /// </summary>
    /// <example>
    ///     <code>
    ///     [DemoSceneBuilder("Scenes/Settings/SettingsDemo.unity")]
    ///     public static void Build(DemoSceneBuildContext ctx) { ... }
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DemoSceneBuilderAttribute : Attribute
    {
        /// <param name="scenePath">Scene path relative to the sample root, e.g. "Scenes/Settings/SettingsDemo.unity".</param>
        public DemoSceneBuilderAttribute(string scenePath)
        {
            ScenePath = scenePath;
        }

        /// <summary>Scene path relative to the sample root (forward slashes, ends with .unity).</summary>
        public string ScenePath { get; }

        /// <summary>Sample the path is relative to. Defaults to <see cref="DemoSample.Demo" />.</summary>
        public DemoSample Sample { get; set; } = DemoSample.Demo;

        /// <summary>Run order when several builders target the same scene (lower runs first).</summary>
        public int Order { get; set; }
    }
}
