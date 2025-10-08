using System;
using System.Collections.Generic;
using Neo.Extensions;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///     ✏️  Universal line-drawing component.
    ///     • Draws a <see cref="LineRenderer" /> with the mouse / first touch,
    ///     • Chaikin-smooths the polyline *live* as you draw,
    ///     • Optional 2-D collider & timed self-destruct,
    ///     • Can clone most settings from a “template” <see cref="LineRenderer" />.
    /// </summary>
    public sealed class Drawer : MonoBehaviour
    {
        /* ───────── INPUT ───────────────────────────────────────────────── */

        [Header("Input")] [Tooltip("If OFF, component ignores all input completely.")]
        public bool isActive = true;

        /* ───────── TEMPLATE (OPTIONAL) ─────────────────────────────────── */

        [Header("Template (Optional)")]
        [Tooltip("When ON and a template is assigned, most visual parameters "
                 + "will be copied from that LineRenderer.")]
        public bool useTemplateSettings;

        public LineRenderer templateRenderer;

        /* ───────── LOOK & FEEL (fallbacks if template not used) ────────── */

        [Header("Line Visual")]
        [Tooltip("Если задан, используется этот материал. Иначе — Sprite, иначе — Texture2D, иначе дефолтный.")]
        public Material lineMaterial;

        [Tooltip("Если материал не задан, используется этот спрайт (текстура спрайта).")]
        public Sprite lineSprite;

        [Tooltip("Если материал и спрайт не заданы, используется эта текстура.")]
        public Texture2D lineTexture;

        [FormerlySerializedAs("colour")] public Gradient color = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        [Header("Width")] [Min(0f)] public float lineWidth = 0.1f;
        public bool useWidthCurve;
        public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Renderer Extras")] public LineTextureMode textureMode = LineTextureMode.Stretch;
        public Vector2 textureScale = Vector2.one;
        public LineAlignment alignment = LineAlignment.View;
        [Range(0, 90)] public int cornerVerts = 10;
        [Range(0, 90)] public int capVerts = 90;
        public bool loop;
        public string sortingLayerName = "Default";
        public int sortingOrder;

        /* ───────── ALGORITHMS ──────────────────────────────────────────── */

        [Header("Algorithms")] public bool useFolow = true;

        [Tooltip("Chaikin smoothing passes each frame.")] [Range(0, 10)]
        public int smoothing = 2;

        public float fixedZ;

        [Min(0)] public int maxPoints = 1000;

        [Min(0)] public float fixedLength;
        //[Min(0.05f)] public float tresholdFixLength = 0.1f;

        [Tooltip("Minimum distance (world units) between raw points.")] [Min(0f)]
        public float minPointDistance = 1f;


        /* ───────── LIFECYCLE & PHYSICS ─────────────────────────────────── */

        [Header("Lifecycle")]
        [Tooltip("0 = keep forever; otherwise the finished line is destroyed after N seconds.")]
        [Min(2)]
        public int minCountCreate = 2;

        [Min(0)] public float minDistanceCreate = 0.2f;

        [Min(0)] public float maxDistanceCreate;

        [Min(0)] public float deleteAfterRelease;

        [Tooltip("0 = keep forever; otherwise the finished line is after N seconds.")] [Min(0)]
        public float drawRelease;


        [Header("Physics")] public bool addCollider;

        public bool colliderAfterCreation;
        /* ───────── EVENTS ──────────────────────────────────────────────── */

        [Header("Events")] public LineCreatedEvent OnLineCreated = new();
        public UnityEvent<float> OnDistanceChanged = new();

        public UnityEvent<int> OnPointChanged = new();

        public UnityEvent<float> OnTimerChanged = new();

        public UnityEvent<float> OnRemainingPercent = new();

        [Tooltip("Событие: начало рисования линии (точка старта)")]
        public UnityEvent<Vector3> OnLineStarted = new();

        public List<LineRenderer> lines = new();

        private LineRenderer _currentLR;
        private float _distance;
        private EdgeCollider2D _liveCol;
        private float _timer;
        private Camera cam;

        /* ───────── INTERNAL ────────────────────────────────────────────── */

        public float Distance
        {
            get => _distance;
            private set
            {
                _distance = value;
                OnDistanceChanged?.Invoke(value);
            }
        }

        public int CountPoints => rawPoints.Count;

        public float Timer
        {
            get => _timer;
            set
            {
                _timer = value;
                OnTimerChanged?.Invoke(value);

                if (drawRelease > 0)
                {
                    var progress = Mathf.Clamp01(1 - value / drawRelease);
                    OnRemainingPercent?.Invoke(progress);
                }
            }
        }

        public List<Vector3> rawPoints { get; } = new();

        /* ───────── UNITY LIFECYCLE ─────────────────────────────────────── */

        private void Awake()
        {
            cam = Camera.main;

            if (!lineMaterial)
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        private void Update()
        {
            if (!isActive) return;

            if (_currentLR != null)
                Timer += Time.deltaTime;

            if (Input.GetMouseButtonDown(0))
                BeginLine(ScreenToWorld(Input.mousePosition));

            if (Input.GetMouseButton(0))
            {
                var worldPos = ScreenToWorld(Input.mousePosition);

                AppendPoint(worldPos);

                Folow(worldPos);

                if (drawRelease > 0 && Timer >= drawRelease) EndLine();
            }

            if (Input.GetMouseButtonUp(0))
                EndLine();
        }

        private void Folow(Vector3 pos)
        {
            if (!useFolow || _currentLR == null || _currentLR.positionCount == 0) return;

            pos.z = fixedZ;
            _currentLR.SetPosition(_currentLR.positionCount - 1, pos);
        }

        /* ───────── CORE LOGIC ──────────────────────────────────────────── */

        /// <summary>
        ///     Initializes a new line by creating the LineRenderer and setting up initial conditions.
        /// </summary>
        /// <param name="position">The starting world position of the line.</param>
        public void BeginLine(Vector3 position)
        {
            _currentLR = CreateLineRenderer();

            ResetPoints();

            AppendPoint(position);

            if (addCollider && !colliderAfterCreation) CreateCollider();

            Timer = 0;
            OnLineStarted?.Invoke(position);
        }

        /// <summary>
        ///     Clears any existing points and resets the LineRenderer's position count.
        /// </summary>
        public void ResetPoints()
        {
            Distance = 0;
            rawPoints.Clear();
            _currentLR.positionCount = 0;
            OnPointChanged?.Invoke(CountPoints);
        }

        /// <summary>
        ///     Creates and configures a LineRenderer. The source of settings depends on the
        ///     flag <see cref="useTemplateSettings" />.
        /// </summary>
        public LineRenderer CreateLineRenderer()
        {
            LineRenderer lr;

            if (useTemplateSettings && templateRenderer != null)
            {
                lr = Instantiate(templateRenderer, transform);
                lr.name = $"Line {Time.frameCount}";
                lr.transform.localPosition = Vector3.zero;
                lr.transform.localRotation = Quaternion.identity;
                lr.transform.localScale = Vector3.one;
            }
            else
            {
                var go = new GameObject($"Line {Time.frameCount}");
                go.transform.SetParent(transform);
                lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
            }

            // --- Для каждой линии создаём свой материал ---
            Material runtimeMat = null;
            if (lineMaterial != null)
            {
                runtimeMat = new Material(lineMaterial); // клон
                if (lineSprite != null)
                    runtimeMat.mainTexture = lineSprite.texture;
                else if (lineTexture != null)
                    runtimeMat.mainTexture = lineTexture;
                lr.material = runtimeMat;
            }
            else if (lineSprite != null)
            {
                runtimeMat = new Material(Shader.Find("Sprites/Default"));
                runtimeMat.mainTexture = lineSprite.texture;
                lr.material = runtimeMat;
            }
            else if (lineTexture != null)
            {
                runtimeMat = new Material(Shader.Find("Sprites/Default"));
                runtimeMat.mainTexture = lineTexture;
                lr.material = runtimeMat;
            }
            // иначе — материал из шаблона или дефолтный

            ApplyFallbackSettings(lr);
            return lr;
        }

        /// <summary>
        ///     Applies settings defined in the inspector to a LineRenderer when no template is used.
        /// </summary>
        public void ApplyFallbackSettings(LineRenderer lr)
        {
            lr.colorGradient = color;
            lr.widthMultiplier = lineWidth;
            if (useWidthCurve) lr.widthCurve = widthCurve;

            lr.textureMode = textureMode;
            lr.textureScale = textureScale;
            lr.alignment = alignment;
            lr.numCornerVertices = cornerVerts;
            lr.numCapVertices = capVerts;
            lr.loop = loop;
            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        /// <summary>
        ///     Adds a new point to the line, applying smoothing if configured.
        /// </summary>
        /// <param name="worldPos">The world position of the point to add.</param>
        public void AppendPoint(Vector3 worldPos)
        {
            if (!_currentLR) return;

            // 1. Сразу приводим Z к единому уровню
            worldPos.z = fixedZ;
            var pos = worldPos;

            if (CountPoints == 0)
            {
                rawPoints.Add(pos);
                OnPointChanged?.Invoke(rawPoints.Count);
                return;
            }

            // 2. Вычисляем конечную позицию с учётом fixedLength
            var last = rawPoints[^1];

            if (fixedLength > 0)
            {
                var dir = worldPos - last;
                if (dir.sqrMagnitude < 1e-6f) return;

                if (Vector3.Distance(last, worldPos) > fixedLength)
                    pos = last + dir.normalized * fixedLength;
                else
                    return;
            }
            else
            {
                if (Vector3.Distance(last, worldPos) < minPointDistance) return;
            }

            // 3. Добавляем точку
            rawPoints.Add(pos);
            OnPointChanged?.Invoke(rawPoints.Count);
            Distance += Vector3.Distance(last, pos);

            // 4. Сглаживаем, ограничиваем, перезаписываем
            var processed = LimitPoints(Smooth(rawPoints, smoothing, fixedZ), maxPoints);
            _currentLR.positionCount = processed.Count;
            _currentLR.SetPositions(processed.ToArray());

            // 5. «Хвост»
            if (useFolow) _currentLR.SetPosition(_currentLR.positionCount - 1, pos);

            // 6. Живой коллайдер
            if (addCollider && !colliderAfterCreation && _liveCol)
                UpdateColliderPoints(_liveCol, processed);

            if (maxDistanceCreate > 0 && Distance >= maxDistanceCreate)
                EndLine();
        }

        public static void UpdateColliderPoints(EdgeCollider2D col, List<Vector3> src)
        {
            var pts2D = new Vector2[src.Count];
            for (var i = 0; i < src.Count; i++) pts2D[i] = src[i];
            col.points = pts2D;
        }

        /// <summary>
        ///     Evenly selects ≤ Max points from the list.
        ///     0 or 1 → returns the source list (without restriction).
        /// </summary>
        public static List<Vector3> LimitPoints(List<Vector3> pts, int max)
        {
            if (max <= 1 || pts.Count <= max) return pts;

            if (max == 2)
                return new List<Vector3> { pts[0], pts[^1] };

            var result = new List<Vector3>(max);
            var step = (pts.Count - 1f) / (max - 1);

            for (var i = 0; i < max; i++)
            {
                var idx = Mathf.RoundToInt(i * step);
                result.Add(pts[idx]);
            }

            return result;
        }

        /// <summary>
        ///     Finalizes and processes the line, including optional collider generation.
        /// </summary>
        public void EndLine()
        {
            if (_currentLR == null) return;

            if (rawPoints.Count < minCountCreate ||
                (minDistanceCreate > 0 && Distance < minDistanceCreate)) // too short – discard
            {
                Destroy(_currentLR.gameObject);
                return;
            }

            // ensure collider gets the smoothed version
            if (addCollider && colliderAfterCreation) CreateCollider();

            lines.Add(_currentLR);
            OnLineCreated.Invoke(_currentLR);

            if (deleteAfterRelease > 0f)
            {
                var line = _currentLR;
                this.Delay(deleteAfterRelease, () => { Delete(line); });
            }

            _currentLR = null;
            rawPoints.Clear();
        }

        public EdgeCollider2D CreateCollider()
        {
            _liveCol = _currentLR.gameObject.AddComponent<EdgeCollider2D>();
            var pts2D = GetCurrentLinePositions();
            _liveCol.points = pts2D;
            _liveCol.edgeRadius = (useWidthCurve ? widthCurve.Evaluate(rawPoints.Count - 1) : lineWidth) * 0.5f;
            return _liveCol;
        }

        private Vector2[] GetCurrentLinePositions()
        {
            var pts2D = new Vector2[_currentLR.positionCount];
            for (var i = 0; i < _currentLR.positionCount; i++)
                pts2D[i] = _currentLR.GetPosition(i);
            return pts2D;
        }

        /// <summary>
        ///     Destroys the provided LineRenderer and removes it from the list of lines.
        /// </summary>
        /// <param name="lr">The LineRenderer to delete.</param>
        public void Delete(LineRenderer lr)
        {
            if (lr == null) return;
            lines.Remove(lr);
            Destroy(lr.gameObject);
        }

        /// <summary>
        ///     Deletes the first line in the list, if any exist.
        /// </summary>
        public void DeleteFirst()
        {
            if (lines.Count == 0) return;
            Delete(lines[0]);
        }

        /// <summary>
        ///     Deletes the last line in the list, if any exist.
        /// </summary>
        public void DeleteLast()
        {
            if (lines.Count == 0) return;
            Delete(lines[^1]);
        }

        /// <summary>
        ///     Destroys all lines and clears the list of LineRenderers.
        /// </summary>

        [Button]
        public void DeleteAll()
        {
            Debug.Log($"[Drawer] DeleteAll called for: {gameObject.name}");
            foreach (var lr in new List<LineRenderer>(lines))
                Destroy(lr.gameObject);
            lines.Clear();
            // Удаляем текущую рисуемую линию, если она ещё не добавлена в lines
            if (_currentLR != null)
            {
                Destroy(_currentLR.gameObject);
                _currentLR = null;
            }

            rawPoints.Clear();
        }

        /* ───────── HELPERS ──────────────────────────────────────────────── */

        /// <summary>
        ///     Converts a screen position to world coordinates based on the main camera's Z position.
        /// </summary>
        /// <param name="screen">The screen space coordinates.</param>
        /// <returns>The converted world space position.</returns>
        public Vector3 ScreenToWorld(Vector3 screen)
        {
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }

        /// <summary>
        ///     Applies the Chaikin smoothing algorithm to a list of points over multiple passes.
        /// </summary>
        /// <param name="points">The original list of points to smooth.</param>
        /// <param name="passes">Number of smoothing passes to apply.</param>
        /// <param name="fixedZ">Optional fixed Z value for the resulting points. Default is 0.</param>
        /// <returns>A new list of smoothed points based on the Chaikin algorithm.</returns>
        public static List<Vector3> Smooth(List<Vector3> points, int passes, float? fixedZ = 0)
        {
            if (passes <= 0 || points.Count < 3) return new List<Vector3>(points);

            List<Vector3> output = new(points);
            for (var p = 0; p < passes; p++)
            {
                var tmp = new List<Vector3> { output[0] };
                for (var i = 0; i < output.Count - 1; i++)
                {
                    var a = output[i];
                    var b = output[i + 1];

                    if (fixedZ != null)
                    {
                        a.z = fixedZ.Value;
                        b.z = fixedZ.Value;
                    }

                    tmp.Add(Vector3.Lerp(a, b, 0.25f));
                    tmp.Add(Vector3.Lerp(a, b, 0.75f));
                }

                tmp.Add(output[^1]);
                output = tmp;
            }

            return output;
        }

        public static float GetDistance(Vector3[] points)
        {
            float distance = 0;

            for (var i = 1; i < points.Length; i++) distance += Vector3.Distance(points[i - 1], points[i]);

            return distance;
        }

        public static float GetDistance(List<Vector3> points)
        {
            float distance = 0;

            for (var i = 1; i < points.Count; i++) distance += Vector3.Distance(points[i - 1], points[i]);

            return distance;
        }

        [Serializable]
        public class LineCreatedEvent : UnityEvent<LineRenderer>
        {
        }
    }
}