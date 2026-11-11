#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play-mode smoke of every scene in the visible sample set: each one loads, every demo controller reports
    ///     <c>IsSceneBuilt</c>, it ticks 12 frames without an error, assert, exception or "scene not authored" warning,
    ///     has no Filled Image without a sprite, and every scene of the Demo download carries its authored info
    ///     card (<c>ModuleDemoSceneInfo</c>). Scenes are discovered, so a new demo is covered without editing this
    ///     file. Hidden <c>Samples~</c> layouts report an explicit Ignore.
    /// </summary>
    public sealed class SampleScenePlaySmokeTests
    {
        private const int TickFrames = 12;

        /// <summary>
        ///     Scenes skipped on purpose, matched by a path fragment inside their sample (works for the developer and
        ///     the imported layout alike), with the reason the skip is honest.
        /// </summary>
        private static readonly Dictionary<string, string> KnownSkips = new Dictionary<string, string>(StringComparer.Ordinal)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            {
                "/Example Scenes/",
                "the Character Movement Fundamentals scenes use CMF CharacterKeyboardInput, CameraMouseInput, " +
                "ClickToMoveController, DemoMenu and MouseCursorLock, which read UnityEngine.Input; they need " +
                "Active Input Handling = Both."
            }
#endif
        };

        private readonly SamplePlaySceneIsolation _isolation = new SamplePlaySceneIsolation();
        private SampleLogCapture _capture;

        private static IEnumerable<string> SampleScenes()
        {
            return SamplePlayScenes.DiscoverScenes();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _capture?.Dispose();
            _capture = null;
            yield return _isolation.End();
        }

        [UnityTest]
        public IEnumerator SampleScene_LoadsTicksWithoutErrorsAndShowsItsGuide(
            [ValueSource(nameof(SampleScenes))] string scenePath)
        {
            // WHY: re-armed in the body because the UTF log scope of [UnitySetUp] does not carry into it.
            LogAssert.ignoreFailingMessages = false;
            if (scenePath == SamplePlayScenes.NoVisibleSampleRoot)
            {
                Assert.Ignore("No sample scene is visible to the AssetDatabase. Rename Assets/Neoxider/Samples~ to " +
                              "Samples during development or import the samples (Assets/Samples/NeoxiderTools/" +
                              "<version>/...). Hidden Samples~ scripts do not compile; SampleSceneCoverageTests " +
                              "checks their YAML.");
            }

            foreach (KeyValuePair<string, string> skip in KnownSkips)
            {
                if (scenePath.IndexOf(skip.Key, StringComparison.Ordinal) >= 0)
                {
                    Assert.Ignore(scenePath + ": " + skip.Value);
                }
            }

            List<string> inHiddenSample = new List<string>();
            List<string> missing = new List<string>();
            SamplePlayScenes.FindUnresolvedReferences(scenePath, inHiddenSample, missing);
            Assert.That(missing, Is.Empty, scenePath + " references script/prefab GUIDs that exist nowhere: " +
                                           string.Join(", ", missing));
            if (inHiddenSample.Count > 0)
            {
                Assert.Ignore(scenePath + " needs " + inHiddenSample.Count + " script/prefab asset(s) from a hidden " +
                              "sample (" + string.Join(", ", inHiddenSample) + "). Make every sample visible to smoke it.");
            }

            _isolation.Begin();
            _capture = new SampleLogCapture();
            yield return SamplePlayScenes.Load(scenePath);
            List<string> failures = new List<string>();
            // WHY: IsSceneBuilt is read right after load, so it reflects the saved authoring before gameplay runs.
            Scene loaded = SceneManager.GetSceneByPath(scenePath);
            int controllers = loaded.isLoaded ? SamplePlayScenes.CollectUnbuiltControllerFailures(loaded, failures) : 0;
            yield return SamplePlayScenes.WaitFrames(TickFrames);
            string[] logFailures = _capture.Failures;
            _capture.Dispose();
            _capture = null;

            foreach (string log in logFailures)
            {
                failures.Add("Logged while loading and ticking " + TickFrames + " frames: " + log);
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            Assert.That(scene.isLoaded, Is.True, scenePath + " must stay loaded; a demo does not switch scenes on " +
                                                 "start.\n" + string.Join("\n", failures));
            Assert.That(scene.rootCount, Is.GreaterThan(0), scenePath);
            TestContext.WriteLine(scenePath + ": " + controllers + " demo controller(s) checked for IsSceneBuilt.");

            SamplePlayScenes.CollectFilledImageFailures(scene, failures);
            int authoredCards = SamplePlayScenes.CollectInfoCardFailures(scene, failures);
            // WHY: NeoxiderPages and Character Movement run without the Demo assembly, so they carry no card.
            if (scenePath.Replace('\\', '/').Contains("/NeoxiderDemos/Demo/") && authoredCards == 0)
            {
                failures.Add("A Demo scene carries no ModuleDemoSceneInfo with an authored info card.");
            }

            TestContext.WriteLine(scenePath + ": " + authoredCards + " authored info card(s).");

            Assert.That(failures, Is.Empty, scenePath + ":\n" + string.Join("\n", failures));
        }
    }
}
#endif
