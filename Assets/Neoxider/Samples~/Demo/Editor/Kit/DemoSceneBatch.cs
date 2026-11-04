using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     Command-line entries for demo scenes. Rebuild headless:
    ///     <code>
    ///     Unity.exe -batchmode -nographics -projectPath &lt;project&gt; -logFile &lt;log&gt;
    ///         -executeMethod Neo.Demo.Editor.DemoSceneBatch.RebuildAll
    ///         [-neoDemoScenes "Scenes/Settings/SettingsDemo.unity;SaveDemo"]
    ///         [-neoDemoInfoCards] [-neoDemoReport &lt;report.json&gt;] [-neoDemoRoot &lt;asset path&gt;]
    ///     </code>
    ///     Capture what the Game view shows, without Play Mode and without saving (needs a GPU: no -nographics):
    ///     <code>
    ///     Unity.exe -batchmode -projectPath &lt;project&gt; -logFile &lt;log&gt;
    ///         -executeMethod Neo.Demo.Editor.DemoSceneBatch.CaptureAll -neoDemoShots &lt;dir&gt;
    ///         [-neoDemoScenes "SettingsDemo;UI.unity"] [-neoDemoShotSize 1920x1080]
    ///     </code>
    ///     Both exit with 0 when every selected scene succeeded, 1 otherwise (failing scenes are logged with
    ///     "[DemoSceneKit] ... FAIL"). Outside batch mode they only log and never close the editor.
    /// </summary>
    public static class DemoSceneBatch
    {
        /// <summary>Output folder of <see cref="CaptureAll" />: one &lt;SceneName&gt;.png per scene.</summary>
        public const string ShotsArgument = "-neoDemoShots";

        /// <summary>Capture size "WIDTHxHEIGHT" (default 1920x1080).</summary>
        public const string ShotSizeArgument = "-neoDemoShotSize";

        /// <summary>Scene filter: ';' or ',' separated, repeatable. Matches asset path, sample-relative path or file name.</summary>
        public const string ScenesArgument = "-neoDemoScenes";

        /// <summary>Also author a standalone info card in every other Demo scene with an unauthored ModuleDemoSceneInfo.</summary>
        public const string InfoCardsArgument = "-neoDemoInfoCards";

        /// <summary>Write a JSON report to this path.</summary>
        public const string ReportArgument = "-neoDemoReport";

        public static void RebuildAll()
        {
            int exitCode = 1;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                List<string> filter = ReadList(args, ScenesArgument);
                bool infoCards = HasFlag(args, InfoCardsArgument);
                DemoSceneRunReport report = DemoSceneRunner.RebuildAll(filter.Count > 0 ? filter : null, infoCards);
                AssetDatabase.SaveAssets();
                string reportPath = DemoSampleRoots.ReadArgument(ReportArgument);
                if (!string.IsNullOrEmpty(reportPath))
                {
                    File.WriteAllText(reportPath, report.ToJson(), new UTF8Encoding(false));
                }

                exitCode = report.Succeeded ? 0 : 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            finally
            {
                Debug.Log(DemoSceneRunner.LogPrefix + "Batch rebuild finished with exit code " + exitCode + ".");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }
        }

        /// <summary>
        ///     Captures every registered demo scene (or the -neoDemoScenes ones) to -neoDemoShots &lt;dir&gt; as
        ///     &lt;SceneName&gt;.png without entering Play Mode and without saving any scene.
        /// </summary>
        public static void CaptureAll()
        {
            int exitCode = 1;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string directory = DemoSampleRoots.ReadArgument(ShotsArgument);
                List<string> filter = ReadList(args, ScenesArgument);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    Debug.LogError(DemoSceneRunner.LogPrefix + "CaptureAll needs " + ShotsArgument + " <output folder>.");
                }
                else if (!TryParseSize(DemoSampleRoots.ReadArgument(ShotSizeArgument), out int width, out int height))
                {
                    Debug.LogError(DemoSceneRunner.LogPrefix + ShotSizeArgument + " must look like 1920x1080, got '" +
                                   DemoSampleRoots.ReadArgument(ShotSizeArgument) + "'.");
                }
                else
                {
                    DemoCaptureReport report = DemoSceneCapture.CaptureScenes(filter.Count > 0 ? filter : null,
                        Path.GetFullPath(directory), width, height);
                    exitCode = report.Succeeded ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            finally
            {
                Debug.Log(DemoSceneRunner.LogPrefix + "Batch capture finished with exit code " + exitCode + ".");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }
        }

        /// <summary>Parses "1920x1080" (also 'X' or '*'); null or empty gives the 1920x1080 default.</summary>
        public static bool TryParseSize(string value, out int width, out int height)
        {
            width = DemoSceneCapture.DefaultWidth;
            height = DemoSceneCapture.DefaultHeight;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string[] parts = value.Trim().Split('x', 'X', '*');
            return parts.Length == 2 &&
                   int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out width) &&
                   int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out height) &&
                   width >= 16 && height >= 16 && width <= 8192 && height <= 8192;
        }

        internal static List<string> ReadList(string[] args, string name)
        {
            List<string> values = new();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string part in args[i + 1].Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string value = part.Trim();
                    if (value.Length > 0)
                    {
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
