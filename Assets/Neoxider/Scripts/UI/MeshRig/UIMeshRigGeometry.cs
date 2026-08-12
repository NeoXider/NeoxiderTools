using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// Renderer-independent coordinates used to build a rig mesh. Size may be negative, which lets an
    /// adapter express a top-left/y-down surface without changing the deformation algorithm.
    /// </summary>
    public readonly struct UIMeshRigCoordinateSpace
    {
        public UIMeshRigCoordinateSpace(Vector2 origin, Vector2 size)
        {
            Origin = origin;
            Size = size;
        }

        public Vector2 Origin { get; }
        public Vector2 Size { get; }

        public Vector2 NormalizedToPosition(Vector2 normalizedPosition)
        {
            return Origin + Vector2.Scale(Size, normalizedPosition);
        }

        public Vector2 PositionToNormalized(Vector2 position)
        {
            return new Vector2(
                SafeInverseLerp(Origin.x, Origin.x + Size.x, position.x),
                SafeInverseLerp(Origin.y, Origin.y + Size.y, position.y));
        }

        private static float SafeInverseLerp(float from, float to, float value)
        {
            float distance = to - from;
            return Mathf.Abs(distance) > 0.000001f ? (value - from) / distance : 0f;
        }
    }

    /// <summary>
    /// One point's binding and current pose, expressed without references to render components.
    /// </summary>
    public readonly struct UIMeshRigPointState
    {
        public UIMeshRigPointState(
            bool enabled,
            Vector2 restCenterNormalized,
            Vector2 innerRadiusNormalized,
            Vector2 outerRadiusNormalized,
            float strength,
            AnimationCurve falloffCurve,
            Vector2 currentCenter,
            float rotationDeltaDegrees,
            Vector2 scaleRatio,
            float positionInfluence = 1f,
            float rotationInfluence = 1f,
            float scaleInfluence = 1f)
        {
            Enabled = enabled;
            RestCenterNormalized = restCenterNormalized;
            InnerRadiusNormalized = innerRadiusNormalized;
            OuterRadiusNormalized = outerRadiusNormalized;
            Strength = Mathf.Max(0f, strength);
            FalloffCurve = falloffCurve;
            CurrentCenter = currentCenter;
            RotationDeltaDegrees = rotationDeltaDegrees;
            ScaleRatio = scaleRatio;
            PositionInfluence = Mathf.Clamp01(positionInfluence);
            RotationInfluence = Mathf.Clamp01(rotationInfluence);
            ScaleInfluence = Mathf.Clamp01(scaleInfluence);
        }

        public bool Enabled { get; }
        public Vector2 RestCenterNormalized { get; }
        public Vector2 InnerRadiusNormalized { get; }
        public Vector2 OuterRadiusNormalized { get; }
        public float Strength { get; }
        public AnimationCurve FalloffCurve { get; }
        public Vector2 CurrentCenter { get; }
        public float RotationDeltaDegrees { get; }
        public Vector2 ScaleRatio { get; }
        public float PositionInfluence { get; }
        public float RotationInfluence { get; }
        public float ScaleInfluence { get; }
    }

    /// <summary>
    /// Renderer-neutral mesh output. Adapters copy these arrays into VertexHelper, MeshWriteData or Mesh.
    /// </summary>
    public sealed class UIMeshRigGeometry
    {
        public UIMeshRigGeometry(Vector3[] vertices, int[] indices, Vector2[] uv)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Indices = indices ?? throw new ArgumentNullException(nameof(indices));
            UV = uv ?? throw new ArgumentNullException(nameof(uv));
        }

        public Vector3[] Vertices { get; }
        public int[] Indices { get; }
        public Vector2[] UV { get; }
    }

    /// <summary>
    /// Pure grid, influence, falloff and pose evaluation shared by every output adapter.
    /// </summary>
    public static class UIMeshRigGeometryBuilder
    {
        public static UIMeshRigGeometry Build(
            int columns,
            int rows,
            UIMeshRigCoordinateSpace space,
            Rect uvRect,
            bool deformationEnabled,
            IReadOnlyList<UIMeshRigPointState> points)
        {
            int safeColumns = Mathf.Clamp(columns, 2, 40);
            int safeRows = Mathf.Clamp(rows, 2, 40);
            int vertexCount = (safeColumns + 1) * (safeRows + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];

            for (int y = 0; y <= safeRows; y++)
            {
                float v = y / (float)safeRows;
                for (int x = 0; x <= safeColumns; x++)
                {
                    float u = x / (float)safeColumns;
                    Vector2 normalized = new Vector2(u, v);
                    int vertexIndex = y * (safeColumns + 1) + x;
                    Vector2 position = DeformPoint(normalized, space, deformationEnabled, points);
                    vertices[vertexIndex] = new Vector3(position.x, position.y, 0f);
                    uv[vertexIndex] = new Vector2(
                        Mathf.Lerp(uvRect.xMin, uvRect.xMax, u),
                        Mathf.Lerp(uvRect.yMin, uvRect.yMax, v));
                }
            }

            int[] indices = new int[safeColumns * safeRows * 6];
            int writeIndex = 0;
            int stride = safeColumns + 1;
            for (int y = 0; y < safeRows; y++)
            {
                for (int x = 0; x < safeColumns; x++)
                {
                    int bottomLeft = y * stride + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + stride;
                    int topRight = topLeft + 1;
                    indices[writeIndex++] = bottomLeft;
                    indices[writeIndex++] = topLeft;
                    indices[writeIndex++] = topRight;
                    indices[writeIndex++] = bottomLeft;
                    indices[writeIndex++] = topRight;
                    indices[writeIndex++] = bottomRight;
                }
            }

            return new UIMeshRigGeometry(vertices, indices, uv);
        }

        public static Vector2 DeformPoint(
            Vector2 normalizedPosition,
            UIMeshRigCoordinateSpace space,
            bool deformationEnabled,
            IReadOnlyList<UIMeshRigPointState> points)
        {
            Vector2 basePosition = space.NormalizedToPosition(normalizedPosition);
            if (!deformationEnabled || points == null || points.Count == 0)
            {
                return basePosition;
            }

            float totalWeight = 0f;
            Vector2 weightedPosition = Vector2.zero;
            for (int index = 0; index < points.Count; index++)
            {
                UIMeshRigPointState point = points[index];
                float weight = EvaluateInfluence(point, normalizedPosition);
                if (weight <= 0f)
                {
                    continue;
                }

                weightedPosition += ApplyPose(point, basePosition, space) * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.00001f)
            {
                return basePosition;
            }

            Vector2 average = weightedPosition / totalWeight;
            return Vector2.LerpUnclamped(basePosition, average, Mathf.Clamp01(totalWeight));
        }

        public static float EvaluateInfluence(UIMeshRigPointState point, Vector2 normalizedPosition)
        {
            if (!point.Enabled || point.Strength <= 0f)
            {
                return 0f;
            }

            Vector2 delta = normalizedPosition - point.RestCenterNormalized;
            float distance = delta.magnitude;
            if (distance <= 0.000001f)
            {
                return point.Strength;
            }

            float outerBoundary = GetEllipseBoundaryDistance(delta, point.OuterRadiusNormalized);
            if (distance >= outerBoundary)
            {
                return 0f;
            }

            float innerBoundary = GetEllipseBoundaryDistance(delta, point.InnerRadiusNormalized);
            if (innerBoundary > 0f && distance <= innerBoundary)
            {
                return point.Strength;
            }

            float edgeT = Mathf.InverseLerp(outerBoundary, innerBoundary, distance);
            AnimationCurve curve = point.FalloffCurve;
            float curveWeight = curve != null && curve.length > 0
                ? Mathf.Clamp01(curve.Evaluate(edgeT))
                : Mathf.Clamp01(edgeT);
            return curveWeight * point.Strength;
        }

        public static Vector2 ApplyPose(
            UIMeshRigPointState point,
            Vector2 sourcePosition,
            UIMeshRigCoordinateSpace space)
        {
            Vector2 restCenter = space.NormalizedToPosition(point.RestCenterNormalized);
            Vector2 scaleRatio = Vector2.LerpUnclamped(Vector2.one, point.ScaleRatio, point.ScaleInfluence);
            Vector2 relative = Vector2.Scale(sourcePosition - restCenter, scaleRatio);
            float rotation = point.RotationDeltaDegrees * point.RotationInfluence * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(rotation);
            float sine = Mathf.Sin(rotation);
            Vector2 rotated = new Vector2(
                relative.x * cosine - relative.y * sine,
                relative.x * sine + relative.y * cosine);
            Vector2 translatedCenter = Vector2.LerpUnclamped(
                restCenter,
                point.CurrentCenter,
                point.PositionInfluence);
            return translatedCenter + rotated;
        }

        public static Rect GetAspectFittedRect(Rect availableRect, float sourceAspect, bool preserveAspect, Vector2 pivot)
        {
            if (!preserveAspect || sourceAspect <= 0f || availableRect.height <= 0f)
            {
                return availableRect;
            }

            float rectAspect = availableRect.width / availableRect.height;
            if (sourceAspect > rectAspect)
            {
                float height = availableRect.width / sourceAspect;
                float offset = (availableRect.height - height) * pivot.y;
                availableRect.y += offset;
                availableRect.height = height;
            }
            else
            {
                float width = availableRect.height * sourceAspect;
                float offset = (availableRect.width - width) * pivot.x;
                availableRect.x += offset;
                availableRect.width = width;
            }

            return availableRect;
        }

        private static float GetEllipseBoundaryDistance(Vector2 direction, Vector2 radius)
        {
            float length = direction.magnitude;
            if (length <= 0.000001f || radius.x <= 0.000001f || radius.y <= 0.000001f)
            {
                return 0f;
            }

            Vector2 unit = direction / length;
            float denominator = Mathf.Sqrt(
                unit.x * unit.x / (radius.x * radius.x) +
                unit.y * unit.y / (radius.y * radius.y));
            return denominator > 0.000001f ? 1f / denominator : 0f;
        }
    }
}
