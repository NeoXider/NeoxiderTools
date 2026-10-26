using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsDrawTests
    {
        private const double NativeFrameTimeoutSeconds = 5d;
        private readonly List<Object> _objects = new();
        private readonly List<Drawer> _drawers = new();
        private readonly HashSet<NeoObjectPool> _pools = new();
        private Drawer _drawer;
        private bool _usesPoolManager;
        private PoolManager _previousPoolManager;
        private bool _previousPoolSearchFailed;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Drawer drawer in _drawers)
            {
                if (drawer != null)
                {
                    drawer.DeleteAll();
                }
            }

            _drawers.Clear();
            foreach (NeoObjectPool pool in _pools)
            {
                pool.Clear();
            }

            _pools.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            _drawer = null;
            if (_usesPoolManager)
            {
                SetPoolManagerState(_previousPoolManager, _previousPoolSearchFailed);
                _previousPoolManager = null;
                _usesPoolManager = false;
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(2, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        public void Smooth_FixedPlaneIncludesEndpointsAndUnsmoothedInputs(int passes, int count)
        {
            List<Vector3> points = new();
            for (int i = 0; i < count; i++)
            {
                points.Add(new Vector3(i, i % 2, i + 4));
            }

            List<Vector3> result = Drawer.Smooth(points, passes, 7f);

            Assert.That(result.Count, Is.EqualTo(count < 3 ? count : count << passes));
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].z, Is.EqualTo(7f));
            }

            for (int i = 0; i < count; i++)
            {
                Assert.That(points[i].z, Is.EqualTo(i + 4), "Smoothing must not edit the caller's points.");
            }
        }

        [Test]
        public void GeometryHelpers_PreserveSpatialEndpointsAndMeasureBothInputForms()
        {
            List<Vector3> points = new()
            {
                new Vector3(0f, 0f, 2f),
                new Vector3(3f, 0f, 2f),
                new Vector3(3f, 4f, 2f)
            };

            List<Vector3> smoothed = Drawer.Smooth(points, 1, null);
            Assert.That(smoothed[0], Is.EqualTo(points[0]));
            Assert.That(smoothed[^1], Is.EqualTo(points[^1]));
            Assert.That(smoothed[1], Is.EqualTo(new Vector3(0.75f, 0f, 2f)));
            CollectionAssert.AreEqual(new[] { points[0], points[^1] }, Drawer.LimitPoints(smoothed, 2));
            Assert.That(Drawer.GetDistance(points), Is.EqualTo(7f));
            Assert.That(Drawer.GetDistance(points.ToArray()), Is.EqualTo(7f));
            Assert.That(Drawer.GetDistance(new List<Vector3>()), Is.Zero);
        }

        [Test]
        public void UpdateColliderPoints_ConvertsWorldPositionsToColliderSpace()
        {
            GameObject go = Track(new GameObject("Draw collider coordinates"));
            go.transform.SetPositionAndRotation(new Vector3(7f, -3f, 0f), Quaternion.Euler(0f, 0f, 30f));
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            EdgeCollider2D collider = go.AddComponent<EdgeCollider2D>();
            Vector3 first = new(1f, 2f, 0f);
            Vector3 last = new(4f, 3f, 0f);

            Drawer.UpdateColliderPoints(collider, new List<Vector3>
            {
                go.transform.TransformPoint(first), go.transform.TransformPoint(last)
            });

            Assert.That(collider.enabled, Is.True);
            Assert.That(Vector2.Distance(collider.points[0], first), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(collider.points[1], last), Is.LessThan(0.0001f));
            Drawer.UpdateColliderPoints(collider, new List<Vector3> { first });
            Assert.That(collider.enabled, Is.False, "A one-point stroke must not retain active collision.");
        }

        [Test]
        public void ResetPoints_WithoutStrokeResetsPublicState()
        {
            Drawer drawer = CreateDrawer();
            drawer.rawPoints.Add(Vector3.one);
            drawer.Distance.Value = 4f;
            drawer.PointCount.Value = 3;

            drawer.ResetPoints();

            Assert.That(drawer.CountPoints, Is.Zero);
            Assert.That(drawer.PointCountValue, Is.Zero);
            Assert.That(drawer.DistanceValue, Is.Zero);
            Assert.That(drawer.CreateCollider(), Is.Null);
        }

        [Test]
        public void ResetPoints_CommitsGeometryAndBothMetricsBeforeOrderedNotifications()
        {
            Drawer drawer = CreateDrawer();
            drawer.addCollider = true;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            LineRenderer line = CurrentLine();
            EdgeCollider2D collider = line.GetComponent<EdgeCollider2D>();
            List<string> events = new();
            drawer.Distance.AddListener(distance =>
            {
                if (distance != 0f)
                {
                    return;
                }

                events.Add("distance");
                Assert.That(drawer.CountPoints, Is.Zero);
                Assert.That(drawer.PointCountValue, Is.Zero);
                Assert.That(line.positionCount, Is.Zero);
                Assert.That(collider.enabled, Is.False);
            });
            drawer.PointCount.AddListener(count =>
            {
                if (count == 0)
                {
                    events.Add("count");
                    Assert.That(drawer.DistanceValue, Is.Zero);
                    Assert.That(line.positionCount, Is.Zero);
                }
            });

            drawer.ResetPoints();

            CollectionAssert.AreEqual(new[] { "distance", "count" }, events);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ResetCallback_PreservesReplacementStroke(bool usePointCount, bool throughBegin)
        {
            Drawer drawer = CreateDrawer();
            drawer.fixedZ = 4f;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            Vector3 replacement = new(2f, 3f, 4f);
            bool replaced = false;
            List<Vector3> started = new();
            drawer.OnLineStarted.AddListener(point => started.Add(point));
            System.Action restart = () =>
            {
                if (replaced)
                {
                    return;
                }

                replaced = true;
                Assert.That(drawer.CountPoints, Is.Zero);
                Assert.That(drawer.PointCountValue, Is.Zero);
                Assert.That(drawer.DistanceValue, Is.Zero);
                Assert.That(CurrentLine().positionCount, Is.Zero);
                drawer.BeginLine(replacement);
            };
            if (usePointCount)
            {
                drawer.PointCount.AddListener(count =>
                {
                    if (count == 0)
                    {
                        restart();
                    }
                });
            }
            else
            {
                drawer.Distance.AddListener(distance =>
                {
                    if (distance == 0f)
                    {
                        restart();
                    }
                });
            }

            if (throughBegin)
            {
                drawer.BeginLine(Vector3.one);
            }
            else
            {
                drawer.ResetPoints();
            }

            Assert.That(replaced, Is.True);
            Assert.That(CurrentLine().positionCount, Is.EqualTo(1));
            Assert.That(CurrentLine().GetPosition(0), Is.EqualTo(replacement));
            CollectionAssert.AreEqual(new[] { replacement }, drawer.rawPoints);
            Assert.That(drawer.PointCountValue, Is.EqualTo(1));
            Assert.That(drawer.DistanceValue, Is.Zero);
            CollectionAssert.AreEqual(new[] { replacement }, started);
        }

        [TestCase("count")]
        [TestCase("timer")]
        [TestCase("remaining")]
        [TestCase("started")]
        public void BeginStartupCallback_DoesNotOverwriteReplacementStroke(string callback)
        {
            Drawer drawer = CreateDrawer();
            drawer.fixedZ = 3f;
            drawer.drawRelease = 2f;
            drawer.addCollider = true;
            Vector3 replacement = new(4f, 5f, 3f);
            bool replaced = false;
            List<Vector3> started = new();
            drawer.OnLineStarted.AddListener(point => started.Add(point));
            System.Action restart = () =>
            {
                if (replaced)
                {
                    return;
                }

                replaced = true;
                drawer.BeginLine(replacement);
                drawer.Timer = 0.5f;
            };
            if (callback == "count")
            {
                drawer.PointCount.AddListener(count =>
                {
                    if (count == 1)
                    {
                        restart();
                    }
                });
            }
            else if (callback == "timer")
            {
                drawer.OnTimerChanged.AddListener(value => restart());
            }
            else if (callback == "remaining")
            {
                drawer.OnRemainingPercent.AddListener(value => restart());
            }
            else
            {
                drawer.OnLineStarted.AddListener(point => restart());
            }

            drawer.BeginLine(Vector3.zero);

            Assert.That(replaced, Is.True);
            Assert.That(CurrentLine().positionCount, Is.EqualTo(1));
            Assert.That(CurrentLine().GetPosition(0), Is.EqualTo(replacement));
            CollectionAssert.AreEqual(new[] { replacement }, drawer.rawPoints);
            Assert.That(drawer.PointCountValue, Is.EqualTo(1));
            Assert.That(drawer.DistanceValue, Is.Zero);
            Assert.That(drawer.Timer, Is.EqualTo(0.5f));
            Assert.That(started.Count, Is.EqualTo(callback == "started" ? 2 : 1));
            Assert.That(started[^1], Is.EqualTo(replacement));
        }

        [Test]
        public void DeleteAll_ResetCallbackPreservesReplacementStroke()
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            bool replaced = false;
            drawer.Distance.AddListener(distance =>
            {
                if (distance == 0f && !replaced)
                {
                    replaced = true;
                    drawer.BeginLine(Vector3.up);
                }
            });

            drawer.DeleteAll();

            Assert.That(replaced, Is.True);
            Assert.That(CurrentLine().positionCount, Is.EqualTo(1));
            Assert.That(CurrentLine().GetPosition(0), Is.EqualTo(Vector3.up));
            Assert.That(drawer.PointCountValue, Is.EqualTo(1));
            Assert.That(drawer.DistanceValue, Is.Zero);
        }

        [Test]
        public void BeginLine_RendersFirstPointAndAcceptsExactFixedLength()
        {
            Drawer drawer = CreateDrawer();
            drawer.fixedZ = 5f;
            drawer.fixedLength = 2f;
            Vector3 started = default;
            drawer.OnLineStarted.AddListener(position => started = position);
            drawer.BeginLine(new Vector3(1f, 2f, 99f));
            LineRenderer line = CurrentLine();

            Assert.That(line.positionCount, Is.EqualTo(1));
            Assert.That(line.GetPosition(0), Is.EqualTo(new Vector3(1f, 2f, 5f)));
            Assert.That(started, Is.EqualTo(line.GetPosition(0)));
            drawer.AppendPoint(new Vector3(3f, 2f, -99f));

            Assert.That(drawer.PointCountValue, Is.EqualTo(2));
            Assert.That(drawer.DistanceValue, Is.EqualTo(2f));
            Assert.That(line.GetPosition(1), Is.EqualTo(new Vector3(3f, 2f, 5f)));
        }

        [Test]
        public void BeginLine_DuringStrokeFinalizesPreviousLine()
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            LineRenderer first = CurrentLine();

            drawer.BeginLine(Vector3.up);

            CollectionAssert.AreEqual(new[] { first }, drawer.lines);
            Assert.That(CurrentLine(), Is.Not.SameAs(first));
            Assert.That(drawer.CountPoints, Is.EqualTo(1));
            Assert.That(drawer.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void EndLine_ListenerCanBeginAnotherStroke()
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            LineRenderer finished = CurrentLine();
            Assert.That(finished.positionCount, Is.EqualTo(2));
            Assert.That(finished.GetPosition(1), Is.EqualTo(Vector3.right));
            drawer.OnLineCreated.AddListener(line => drawer.BeginLine(Vector3.up));

            drawer.EndLine();
            drawer.AppendPoint(Vector3.one);

            CollectionAssert.AreEqual(new[] { finished }, drawer.lines);
            Assert.That(drawer.CountPoints, Is.EqualTo(2));
            Assert.That(drawer.rawPoints[0], Is.EqualTo(Vector3.up));
            Assert.That(CurrentLine().GetPosition(1), Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(drawer.rawPoints[1], Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(drawer.PointCountValue, Is.EqualTo(2));
            Assert.That(drawer.DistanceValue, Is.EqualTo(1f));
        }

        [Test]
        public void BeginLine_ReentrantCompletionDoesNotOrphanTheListenerStroke()
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            LineRenderer finished = CurrentLine();
            Assert.That(finished.positionCount, Is.EqualTo(2));
            drawer.OnLineCreated.AddListener(line => drawer.BeginLine(Vector3.up));

            drawer.BeginLine(Vector3.one);

            Assert.That(drawer.lines.Count, Is.EqualTo(1));
            Assert.That(drawer.lines[0], Is.SameAs(finished));
            Assert.That(drawer.transform.childCount, Is.EqualTo(2));
            Assert.That(drawer.rawPoints[0], Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(CurrentLine(), Is.Not.SameAs(finished));
            Assert.That(CurrentLine().positionCount, Is.EqualTo(1));
            Assert.That(CurrentLine().GetPosition(0), Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(drawer.PointCountValue, Is.EqualTo(1));
            Assert.That(drawer.DistanceValue, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MetricListener_CanCompleteAStrokeWithCommittedGeometryAndDistance(bool usePointCount)
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            LineRenderer line = CurrentLine();
            float publishedDistance = 0f;
            drawer.Distance.AddListener(distance => publishedDistance = distance);
            if (usePointCount)
            {
                drawer.PointCount.AddListener(count =>
                {
                    if (count == 2)
                    {
                        drawer.EndLine();
                    }
                });
            }
            else
            {
                drawer.Distance.AddListener(distance => drawer.EndLine());
            }

            drawer.AppendPoint(Vector3.right);

            CollectionAssert.AreEqual(new[] { line }, drawer.lines);
            Assert.That(drawer.DistanceValue, Is.EqualTo(1f));
            Assert.That(publishedDistance, Is.EqualTo(1f));
            Assert.That(drawer.PointCountValue, Is.EqualTo(2));
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(1), Is.EqualTo(Vector3.right));
        }

        [Test]
        public void DeleteAll_RemovesDestroyedEntriesAndResetsReactiveValues()
        {
            Drawer drawer = CreateDrawer();
            drawer.lineMaterial = NewMaterial();
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            Material owned = drawer.lines[0].sharedMaterial;
            Object.DestroyImmediate(drawer.lines[0].gameObject);

            drawer.DeleteAll();

            Assert.That(drawer.lines, Is.Empty);
            Assert.That(drawer.CountPoints, Is.Zero);
            Assert.That(drawer.PointCountValue, Is.Zero);
            Assert.That(drawer.DistanceValue, Is.Zero);
            Assert.That(owned == null, Is.True, "The material outlives an externally destroyed renderer until cleanup.");
        }

        [Test]
        public void ScreenProjection_IntersectsFixedZWithAnAngledPerspectiveCamera()
        {
            Drawer drawer = CreateDrawer();
            drawer.fixedZ = 5f;
            Camera camera = drawer.InputCamera;
            camera.orthographic = false;
            camera.pixelRect = new Rect(0f, 0f, 640f, 480f);
            camera.transform.position = new Vector3(3f, 4f, -10f);
            Vector3 expected = new(1f, 2f, 5f);
            camera.transform.LookAt(expected);
            Vector3 screen = camera.WorldToScreenPoint(expected);

            Assert.That(drawer.TryScreenToWorld(screen, out Vector3 world), Is.True);
            Assert.That(Vector3.Distance(world, expected), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(drawer.ScreenToWorld(screen), expected), Is.LessThan(0.001f));
            camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            Assert.That(drawer.TryScreenToWorld(screen, out world), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => drawer.ScreenToWorld(screen));
            drawer.InputCamera = null;
            Assert.That(drawer.TryScreenToWorld(screen, out world), Is.False);
        }

        [Test]
        public void Template_UsesWorldPositionsAndPreservesBorrowedMaterialOnDelete()
        {
            Drawer drawer = CreateDrawer();
            drawer.transform.position = new Vector3(8f, 3f, 0f);
            GameObject template = Track(new GameObject("Draw template"));
            LineRenderer renderer = template.AddComponent<LineRenderer>();
            Material source = NewMaterial();
            renderer.sharedMaterial = source;
            renderer.useWorldSpace = false;
            drawer.useTemplateSettings = true;
            drawer.templateRenderer = renderer;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer line = drawer.lines[0];

            Assert.That(line.useWorldSpace, Is.True);
            Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.zero));
            Assert.That(line.sharedMaterial, Is.SameAs(source));
            drawer.Delete(line);

            Assert.That(source != null, Is.True, "Runtime materials supplied by callers are borrowed too.");
            Assert.That(renderer.sharedMaterial, Is.SameAs(source));
        }

        [Test]
        public void PooledLine_AppliesTextureAndReturnsToItsPoolAfterFlagChanges()
        {
            Drawer drawer = CreateDrawer();
            PreparePool();
            Material source = NewMaterial();
            Texture2D texture = Track(new Texture2D(2, 2));
            drawer.lineMaterial = source;
            drawer.lineTexture = texture;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer line = drawer.lines[0];
            PooledObjectInfo info = RememberPool(line);
            Material owned = line.sharedMaterial;

            Assert.That(owned, Is.Not.SameAs(source));
            Assert.That(owned.mainTexture, Is.SameAs(texture));
            drawer.usePooling = false;
            drawer.Delete(line);

            Assert.That(info.IsInPool, Is.True);
            Assert.That(line.gameObject.activeSelf, Is.False);
            Assert.That(owned == null, Is.True);
            Assert.That(source != null, Is.True);
        }

        [Test]
        public void PooledCollider_IsReusedAndDisabledWhenNextStrokeHasNoCollider()
        {
            Drawer drawer = CreateDrawer();
            PreparePool();
            drawer.addCollider = true;
            drawer.useWidthCurve = true;
            drawer.lineWidth = 0.2f;
            drawer.widthCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer first = drawer.lines[0];
            RememberPool(first);
            EdgeCollider2D collider = first.GetComponent<EdgeCollider2D>();
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.edgeRadius, Is.EqualTo(0.05f).Within(0.0001f));
            drawer.Delete(first);
            drawer.BeginLine(Vector3.up);
            Assert.That(CurrentLine(), Is.SameAs(first));
            Assert.That(collider.enabled, Is.False);
            drawer.AppendPoint(Vector3.one);
            Assert.That(drawer.CreateCollider(), Is.SameAs(collider));
            Assert.That(first.GetComponents<EdgeCollider2D>().Length, Is.EqualTo(1));
            drawer.EndLine();
            drawer.Delete(first);
            drawer.addCollider = false;
            drawer.useWidthCurve = false;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);

            Assert.That(collider.enabled, Is.False);
            Assert.That(first.widthCurve.Evaluate(0.5f), Is.EqualTo(1f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExternalReturn_OldDrawerCannotMutateTheNextDrawersLease(bool completeFirst)
        {
            Drawer first = CreateDrawer();
            PreparePool();
            Material borrowed = NewMaterial();
            first.poolPrefab.sharedMaterial = borrowed;
            first.lineMaterial = NewMaterial();
            first.addCollider = true;
            first.BeginLine(Vector3.zero);
            first.AppendPoint(Vector3.right);
            LineRenderer line = CurrentLine();
            PooledObjectInfo info = RememberPool(line);
            Material oldOwned = line.sharedMaterial;
            if (completeFirst)
            {
                first.EndLine();
            }

            PoolManager.Release(line.gameObject);
            Assert.That(oldOwned == null, Is.True, "External return must free the old generation before reuse.");
            Assert.That(line.sharedMaterial, Is.SameAs(borrowed));
            Assert.That(first.lines, Is.Empty);
            Drawer second = CreateDrawer();
            second.usePooling = true;
            second.poolPrefab = first.poolPrefab;
            second.lineMaterial = NewMaterial();
            second.addCollider = true;
            second.lineWidth = 0.8f;
            second.BeginLine(Vector3.up);
            second.AppendPoint(Vector3.one);
            Assert.That(CurrentLine(), Is.SameAs(line));
            Material newOwned = line.sharedMaterial;
            int generation = info.SpawnGeneration;
            EdgeCollider2D collider = line.GetComponent<EdgeCollider2D>();
            Assert.That(info.IsInPool, Is.False);
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.up));
            Assert.That(line.GetPosition(1), Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(newOwned != null, Is.True);
            Assert.That(collider.enabled, Is.True);

            first.AppendPoint(Vector3.left * 10f);
            first.ResetPoints();
            first.lineWidth = 10f;
            first.ApplyFallbackSettings(line);
            Assert.That(first.CreateCollider(), Is.Null);
            first.EndLine();
            first.Delete(line);
            first.DeleteAll();
            Object.DestroyImmediate(first.gameObject);

            Assert.That(info.SpawnGeneration, Is.EqualTo(generation));
            Assert.That(info.IsInPool, Is.False);
            Assert.That(line.gameObject.activeSelf, Is.True);
            Assert.That(line.transform.parent, Is.SameAs(second.transform));
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.up));
            Assert.That(line.GetPosition(1), Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(line.widthMultiplier, Is.EqualTo(0.8f));
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.edgeRadius, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(line.sharedMaterial, Is.SameAs(newOwned));
            Assert.That(newOwned != null, Is.True);
            Assert.That(borrowed != null, Is.True);
            second.Delete(line);
            Assert.That(newOwned == null, Is.True);
            Assert.That(line.sharedMaterial, Is.SameAs(borrowed));
        }

        [Test]
        public void Delete_ResetListenerCanReturnAndReissueTheCapturedRenderer()
        {
            Drawer first = CreateDrawer();
            PreparePool();
            first.lineMaterial = NewMaterial();
            first.BeginLine(Vector3.zero);
            first.AppendPoint(Vector3.right);
            LineRenderer line = CurrentLine();
            PooledObjectInfo info = RememberPool(line);
            int previousGeneration = info.SpawnGeneration;
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(1), Is.EqualTo(Vector3.right));
            Drawer second = CreateDrawer();
            second.usePooling = true;
            second.poolPrefab = first.poolPrefab;
            second.lineMaterial = NewMaterial();
            bool reissued = false;
            Material replacementMaterial = null;
            first.Distance.AddListener(distance =>
            {
                if (distance == 0f && !reissued)
                {
                    reissued = true;
                    PoolManager.Release(line.gameObject);
                    second.BeginLine(Vector3.up);
                    second.AppendPoint(Vector3.one);
                    Assert.That(CurrentLine(), Is.SameAs(line));
                    Assert.That(line.positionCount, Is.EqualTo(2));
                    Assert.That(line.GetPosition(1), Is.EqualTo(new Vector3(1f, 1f, 0f)));
                    replacementMaterial = line.sharedMaterial;
                    Assert.That(replacementMaterial != null, Is.True);
                }
            });

            first.Delete(line);

            Assert.That(reissued, Is.True);
            Assert.That(CurrentLine(), Is.SameAs(line));
            Assert.That(info.SpawnGeneration, Is.GreaterThan(previousGeneration));
            Assert.That(info.IsInPool, Is.False);
            Assert.That(line.gameObject.activeSelf, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.up));
            Assert.That(line.GetPosition(1), Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(line.sharedMaterial != null, Is.True);
            Assert.That(line.sharedMaterial, Is.SameAs(replacementMaterial));
            Assert.That(line.sharedMaterial, Is.Not.SameAs(second.lineMaterial));
        }

        [Test]
        public void ExternalReturnAndCapacityEviction_FreeExactClonesAndPreserveBorrowedMaterial()
        {
            Drawer drawer = CreateDrawer();
            PreparePool(0, 1);
            Material borrowed = NewMaterial();
            drawer.poolPrefab.sharedMaterial = borrowed;
            drawer.lineMaterial = NewMaterial();
            LineRenderer retained = drawer.CreateLineRenderer();
            LineRenderer evicted = drawer.CreateLineRenderer();
            PooledObjectInfo retainedInfo = RememberPool(retained);
            PooledObjectInfo evictedInfo = RememberPool(evicted);
            Material retainedOwned = retained.sharedMaterial;
            Material evictedOwned = evicted.sharedMaterial;

            retainedInfo.Return();
            evictedInfo.OwnerPool.ReturnObject(evicted.gameObject);

            Assert.That(retainedInfo.OwnerPool.CountInactive, Is.EqualTo(1));
            Assert.That(retained.sharedMaterial, Is.SameAs(borrowed));
            Assert.That(retainedOwned == null, Is.True);
            Assert.That(evictedOwned == null, Is.True);
            Assert.That(evicted == null, Is.True);
            Assert.That(borrowed != null, Is.True);
            GameObject newer = retainedInfo.OwnerPool.GetObject(Vector3.one, Quaternion.identity);
            Assert.That(newer, Is.SameAs(retained.gameObject));
            drawer.Delete(retained);
            Assert.That(newer.activeSelf, Is.True);
            Assert.That(retainedInfo.IsInPool, Is.False);
            Assert.That(retained.sharedMaterial, Is.SameAs(borrowed));
            retainedInfo.Return();
        }

        [UnityTest]
        public IEnumerator AutomaticPool_OneHundredOwnerDisposalsReturnObjectsToBaseline()
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            Drawer shared = CreateDrawer();
            PreparePool();
            shared.poolPrefab.name = "LineRenderer_PoolPrefab";
            shared.lineMaterial = NewMaterial();
            shared.BeginLine(Vector3.zero);
            shared.AppendPoint(Vector3.right);
            shared.EndLine();
            LineRenderer sharedLine = shared.lines[0];
            PooledObjectInfo sharedInfo = RememberPool(sharedLine);
            Material sharedOwned = sharedLine.sharedMaterial;
            int baseline = Resources.FindObjectsOfTypeAll<PooledObjectInfo>().Length;
            int sourceBaseline = CountAutomaticSources();
            for (int i = 0; i < 100; i++)
            {
                Drawer owner = CreateDrawer();
                owner.usePooling = true;
                owner.lineMaterial = shared.lineMaterial;
                owner.BeginLine(Vector3.zero);
                owner.AppendPoint(Vector3.right);
                LineRenderer line = CurrentLine();
                NeoObjectPool pool = RememberPool(line).OwnerPool;
                Material owned = line.sharedMaterial;
                GameObject source = AutomaticSource(owner);
                int nativeUpdates = 0;
                // Subscribe after BeginLine's explicit Timer=0 notification. Only a real Update can
                // increment this counter before disposal; no private lifecycle method is invoked.
                owner.OnTimerChanged.AddListener(value => nativeUpdates++);
                owner.isActive = true;
                int admissionFrame = Time.frameCount;
                double admissionDeadline = Time.realtimeSinceStartupAsDouble + NativeFrameTimeoutSeconds;
                while ((Time.frameCount <= admissionFrame || nativeUpdates == 0)
                       && Time.realtimeSinceStartupAsDouble < admissionDeadline)
                {
                    yield return null;
                }

                owner.isActive = false;
                Assert.That(Application.isPlaying, Is.True, "Native admission cycle " + i);
                Assert.That(Time.frameCount, Is.GreaterThan(admissionFrame));
                Assert.That(nativeUpdates, Is.GreaterThan(0), "Drawer.Update was not admitted in cycle " + i);
                Assert.That(owner.isActiveAndEnabled, Is.True);
                Assert.That(source != null, Is.True);
                Assert.That(pool.IsRetired, Is.False);
                Assert.That(line.positionCount, Is.EqualTo(2));
                Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.zero));
                Assert.That(line.GetPosition(1), Is.EqualTo(Vector3.right));
                owner.EndLine();
                CollectionAssert.AreEqual(new[] { line }, owner.lines);

                int disposalFrame = Time.frameCount;
                Object.Destroy(owner.gameObject);
                double disposalDeadline = Time.realtimeSinceStartupAsDouble + NativeFrameTimeoutSeconds;
                while (Time.frameCount - disposalFrame < 2
                       && Time.realtimeSinceStartupAsDouble < disposalDeadline)
                {
                    yield return null;
                }

                Assert.That(Application.isPlaying, Is.True);
                Assert.That(Time.frameCount - disposalFrame, Is.GreaterThanOrEqualTo(2));
                Assert.That(owner == null, Is.True, "Owner destruction cycle " + i);
                Assert.That(pool.IsRetired, Is.True, "Automatic source cycle " + i);
                Assert.That(pool.CountInactive, Is.Zero);
                Assert.That(pool.GetObject(Vector3.zero, Quaternion.identity), Is.Null);
                Assert.That(PoolManager.RetirePool(source), Is.False, "The exact registration was already removed.");
                Assert.That(source == null, Is.True);
                Assert.That(owned == null, Is.True);
                Assert.That(Resources.FindObjectsOfTypeAll<PooledObjectInfo>().Length, Is.EqualTo(baseline));
                Assert.That(CountAutomaticSources(), Is.EqualTo(sourceBaseline));
            }

            Assert.That(sharedInfo.OwnerPool.IsRetired, Is.False);
            Assert.That(sharedInfo.IsInPool, Is.False);
            Assert.That(sharedLine.gameObject.activeSelf, Is.True);
            Assert.That(sharedLine.sharedMaterial, Is.SameAs(sharedOwned));
            Assert.That(sharedOwned != null, Is.True);
            GameObject nextShared = PoolManager.Get(shared.poolPrefab.gameObject, Vector3.one, Quaternion.identity);
            Assert.That(nextShared, Is.Not.Null);
            PoolManager.Release(nextShared);
        }

        [UnityTest]
        public IEnumerator AutomaticPool_ExternalOutstandingRentalSurvivesUntilLateReturn()
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            Drawer drawer = CreateDrawer();
            PreparePool();
            drawer.poolPrefab = null;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            int nativeUpdates = 0;
            drawer.OnTimerChanged.AddListener(value => nativeUpdates++);
            drawer.isActive = true;
            int admissionFrame = Time.frameCount;
            double admissionDeadline = Time.realtimeSinceStartupAsDouble + NativeFrameTimeoutSeconds;
            while ((Time.frameCount <= admissionFrame || nativeUpdates == 0)
                   && Time.realtimeSinceStartupAsDouble < admissionDeadline)
            {
                yield return null;
            }

            drawer.isActive = false;
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.frameCount, Is.GreaterThan(admissionFrame));
            Assert.That(nativeUpdates, Is.GreaterThan(0), "Drawer.Update must run before testing native destruction.");
            Assert.That(drawer.isActiveAndEnabled, Is.True);
            drawer.EndLine();
            LineRenderer line = drawer.lines[0];
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(0), Is.EqualTo(Vector3.zero));
            Assert.That(line.GetPosition(1), Is.EqualTo(Vector3.right));
            PooledObjectInfo info = RememberPool(line);
            NeoObjectPool pool = info.OwnerPool;
            GameObject source = AutomaticSource(drawer);
            Assert.That(source != null, Is.True);
            Assert.That(pool.IsRetired, Is.False);
            drawer.Delete(line);
            GameObject outstanding = Track(pool.GetObject(Vector3.one, Quaternion.identity));
            Assert.That(outstanding, Is.SameAs(line.gameObject));
            Assert.That(info.OwnerPool, Is.SameAs(pool));
            Assert.That(info.IsInPool, Is.False);
            int externalGeneration = info.SpawnGeneration;
            Material borrowed = NewMaterial();
            outstanding.GetComponent<LineRenderer>().sharedMaterial = borrowed;

            int disposalFrame = Time.frameCount;
            Object.Destroy(drawer.gameObject);
            double disposalDeadline = Time.realtimeSinceStartupAsDouble + NativeFrameTimeoutSeconds;
            while (Time.frameCount - disposalFrame < 2
                   && Time.realtimeSinceStartupAsDouble < disposalDeadline)
            {
                yield return null;
            }

            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.frameCount - disposalFrame, Is.GreaterThanOrEqualTo(2));
            Assert.That(drawer == null, Is.True);
            Assert.That(source == null, Is.True);
            Assert.That(pool.IsRetired, Is.True);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(outstanding != null, Is.True);
            Assert.That(outstanding.activeSelf, Is.True);
            Assert.That(info.IsInPool, Is.False);
            Assert.That(info.SpawnGeneration, Is.EqualTo(externalGeneration));
            Assert.That(outstanding.GetComponent<LineRenderer>().sharedMaterial, Is.SameAs(borrowed));
            int returnFrame = Time.frameCount;
            PoolManager.Release(outstanding);
            Assert.That(info.IsInPool, Is.True, "The current external generation must enter its valid return path.");
            Assert.That(outstanding.activeSelf, Is.False);
            Assert.That(pool.CountInactive, Is.Zero);
            double returnDeadline = Time.realtimeSinceStartupAsDouble + NativeFrameTimeoutSeconds;
            while (Time.frameCount - returnFrame < 2
                   && Time.realtimeSinceStartupAsDouble < returnDeadline)
            {
                yield return null;
            }

            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.frameCount - returnFrame, Is.GreaterThanOrEqualTo(2));
            Assert.That(outstanding == null, Is.True);
            Assert.That(borrowed != null, Is.True);
        }

        [UnityTest]
        public IEnumerator DestroyDrawer_ReleasesOwnedMaterialsAndReturnsPooledStroke()
        {
            yield return new EnterPlayMode();
            Drawer drawer = CreateDrawer();
            PreparePool();
            Material source = NewMaterial();
            drawer.lineMaterial = source;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer finished = drawer.lines[0];
            PooledObjectInfo finishedInfo = RememberPool(finished);
            Material finishedMaterial = finished.sharedMaterial;
            drawer.BeginLine(Vector3.up);
            LineRenderer active = CurrentLine();
            PooledObjectInfo activeInfo = RememberPool(active);
            Material activeMaterial = active.sharedMaterial;
            Material replacement = NewMaterial();
            drawer.lineMaterial = replacement;

            Object.Destroy(drawer);
            yield return null;
            yield return null;

            Assert.That(finishedMaterial == null, Is.True);
            Assert.That(activeMaterial == null, Is.True);
            Assert.That(source != null, Is.True);
            Assert.That(replacement != null, Is.True);
            Assert.That(finishedInfo.IsInPool, Is.True);
            Assert.That(activeInfo.IsInPool, Is.True);
        }

        [UnityTest]
        public IEnumerator ManualDelete_CancelsOldExpiryBeforePooledReuse()
        {
            yield return new EnterPlayMode();
            Drawer drawer = CreateDrawer();
            PreparePool();
            drawer.deleteAfterRelease = 0.05f;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer line = drawer.lines[0];
            PooledObjectInfo info = RememberPool(line);
            drawer.Delete(line);
            drawer.deleteAfterRelease = 0f;
            drawer.BeginLine(Vector3.up);
            drawer.AppendPoint(Vector3.one);
            drawer.EndLine();

            yield return new WaitForSeconds(0.15f);

            Assert.That(info.IsInPool, Is.False);
            Assert.That(line.gameObject.activeSelf, Is.True);
            CollectionAssert.AreEqual(new[] { line }, drawer.lines);
        }

        [UnityTest]
        public IEnumerator Expiry_DoesNotReturnANewerExternalPoolLease()
        {
            yield return new EnterPlayMode();
            Drawer drawer = CreateDrawer();
            PreparePool();
            drawer.deleteAfterRelease = 0.05f;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            drawer.EndLine();
            LineRenderer line = drawer.lines[0];
            PooledObjectInfo info = RememberPool(line);
            int generation = info.SpawnGeneration;
            PoolManager.Release(line.gameObject);
            GameObject newerLease = info.OwnerPool.GetObject(Vector3.one, Quaternion.identity);
            Assert.That(newerLease, Is.SameAs(line.gameObject));
            Assert.That(info.SpawnGeneration, Is.GreaterThan(generation));

            yield return new WaitForSeconds(0.15f);

            Assert.That(info.IsInPool, Is.False);
            Assert.That(newerLease.activeSelf, Is.True);
            Assert.That(drawer.lines, Is.Empty);
            PoolManager.Release(newerLease);
        }

        private Drawer CreateDrawer()
        {
            GameObject cameraObject = Track(new GameObject("Draw audit camera"));
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject go = Track(new GameObject("Draw audit owner"));
            go.SetActive(false);
            _drawer = go.AddComponent<Drawer>();
            _drawers.Add(_drawer);
            _drawer.InputCamera = camera;
            _drawer.isActive = false;
            _drawer.minPointDistance = 0.1f;
            _drawer.smoothing = 0;
            go.SetActive(true);
            return _drawer;
        }

        private void PreparePool(int initialSize = 10, int maxSize = 100)
        {
            _previousPoolManager = (PoolManager)typeof(Singleton<PoolManager>)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _previousPoolSearchFailed = (bool)typeof(Singleton<PoolManager>)
                .GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            SetPoolManagerState(null, false);
            _usesPoolManager = true;
            PoolManager manager = Track(new GameObject("Draw audit pool manager")).AddComponent<PoolManager>();
            typeof(PoolManager).GetField("_defaultInitialSize", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, initialSize);
            typeof(PoolManager).GetField("_defaultMaxSize", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, maxSize);
            GameObject prefab = Track(new GameObject("Draw audit line prefab"));
            prefab.SetActive(false);
            _drawer.poolPrefab = prefab.AddComponent<LineRenderer>();
            _drawer.usePooling = true;
        }

        private PooledObjectInfo RememberPool(LineRenderer line)
        {
            PooledObjectInfo info = line.GetComponent<PooledObjectInfo>();
            Assert.That(info, Is.Not.Null);
            Assert.That(info.OwnerPool, Is.Not.Null);
            _pools.Add(info.OwnerPool);
            return info;
        }

        private LineRenderer CurrentLine()
        {
            LineRenderer[] renderers = _drawer.GetComponentsInChildren<LineRenderer>();
            return renderers[^1];
        }

        private Material NewMaterial()
        {
            return Track(new Material(Shader.Find("Sprites/Default")));
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static GameObject AutomaticSource(Drawer drawer)
        {
            return (GameObject)typeof(Drawer).GetField("_autoPoolPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(drawer);
        }

        private static int CountAutomaticSources()
        {
            int count = 0;
            foreach (LineRenderer line in Resources.FindObjectsOfTypeAll<LineRenderer>())
            {
                if (line.name == "LineRenderer_PoolPrefab" && line.GetComponent<PooledObjectInfo>() == null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetPoolManagerState(PoolManager instance, bool searchFailed)
        {
            typeof(Singleton<PoolManager>).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, instance);
            typeof(Singleton<PoolManager>).GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, searchFailed);
        }
    }
}
