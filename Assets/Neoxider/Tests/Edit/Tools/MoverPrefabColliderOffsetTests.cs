using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Guards the one authoring invariant of the bundled CMF character controller: every packaged
    ///     asset that carries a <c>Mover</c> must use <c>colliderOffset.y = 0.5</c>.
    ///     <para>
    ///         <c>colliderOffset</c> is normalised — <c>Mover</c> multiplies it by <c>colliderHeight</c> —
    ///         and ground detection parks the transform origin at
    ///         <c>colliderHeight * (0.5 - colliderOffset.y)</c> above the floor. Only <c>0.5</c> puts the
    ///         origin at the character's feet; anything else leaves every child authored feet-at-origin
    ///         (model, camera pivot, spawn points) hanging in the air. 10.4.1 fixed two shipped presets
    ///         that had <c>0</c>.
    ///     </para>
    ///     <para>
    ///         Deliberately text-based rather than going through <c>AssetDatabase</c>: <c>Samples~</c> is
    ///         hidden from Unity's asset database by its tilde, so 16 of the 22 assets this covers are
    ///         invisible to a typed scan — and <c>colliderOffset</c> is a private serialized field that
    ///         would need reflection anyway.
    ///     </para>
    /// </summary>
    public sealed class MoverPrefabColliderOffsetTests
    {
        private const string PackageRoot = "Assets/Neoxider";

        private const float ExpectedOffsetY = 0.5f;

        private const float Tolerance = 0.0001f;

        private static readonly Regex ColliderOffsetPattern = new Regex(
            @"colliderOffset:\s*\{x:\s*(?<x>[^,]+),\s*y:\s*(?<y>[^,]+),\s*z:\s*(?<z>[^}]+)\}",
            RegexOptions.Compiled);

        [Test]
        public void EveryPackagedMover_ParksTheTransformOriginAtTheFeet()
        {
            var offenders = new List<string>();
            int moverCount = 0;

            foreach (string file in EnumerateSerializedAssets())
            {
                foreach (Match match in ColliderOffsetPattern.Matches(File.ReadAllText(file)))
                {
                    moverCount++;

                    string raw = match.Groups["y"].Value.Trim();
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    {
                        offenders.Add($"{Normalise(file)}: cannot parse colliderOffset.y from '{raw}'");
                        continue;
                    }

                    if (Mathf.Abs(y - ExpectedOffsetY) > Tolerance)
                    {
                        offenders.Add(
                            $"{Normalise(file)}: Mover.colliderOffset.y is {y.ToString(CultureInfo.InvariantCulture)}, " +
                            $"expected {ExpectedOffsetY.ToString(CultureInfo.InvariantCulture)} — the transform origin " +
                            "would sit off the floor and every child authored feet-at-origin would float.");
                    }
                }
            }

            // WHY: a guard that finds nothing must fail loudly rather than pass. If the package layout
            // moves, this is the line that says so instead of going quietly green on zero assets.
            Assert.That(moverCount, Is.GreaterThan(0),
                $"No Mover assets found under '{PackageRoot}' — this guard is scanning the wrong path.");

            Assert.That(offenders, Is.Empty,
                "Mover assets with a non-feet-at-origin collider offset:\n  " + string.Join("\n  ", offenders));
        }

        private static IEnumerable<string> EnumerateSerializedAssets()
        {
            foreach (string pattern in new[] { "*.prefab", "*.unity" })
            {
                foreach (string file in Directory.GetFiles(PackageRoot, pattern, SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private static string Normalise(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
