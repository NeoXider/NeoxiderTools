using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// Ready-made point layouts for common UI-sprite deformation jobs.
    /// </summary>
    public enum UIMeshRigLayoutPreset
    {
        SimpleBounce = 0,
        Character = 1,
        FlagCloth = 2
    }

    /// <summary>
    /// Scene-reference-free point data used by both runtime code and editor authoring tools.
    /// </summary>
    public readonly struct UIMeshRigPointLayout
    {
        public UIMeshRigPointLayout(
            string name,
            Vector2 centerNormalized,
            Vector2 innerRadiusNormalized,
            Vector2 outerRadiusNormalized,
            UIMeshRigMotionPreset motionPreset,
            float phase,
            int seed,
            float strength = 1f)
        {
            Name = name;
            CenterNormalized = centerNormalized;
            InnerRadiusNormalized = innerRadiusNormalized;
            OuterRadiusNormalized = outerRadiusNormalized;
            MotionPreset = motionPreset;
            Phase = phase;
            Seed = seed;
            Strength = strength;
        }

        public string Name { get; }
        public Vector2 CenterNormalized { get; }
        public Vector2 InnerRadiusNormalized { get; }
        public Vector2 OuterRadiusNormalized { get; }
        public UIMeshRigMotionPreset MotionPreset { get; }
        public float Phase { get; }
        public int Seed { get; }
        public float Strength { get; }
    }

    /// <summary>
    /// Pure preset lookup. Use <see cref="UIMeshRigLayoutBuilder"/> to instantiate a complete rig.
    /// </summary>
    public static class UIMeshRigLayoutPresets
    {
        public static int GetPointCount(UIMeshRigLayoutPreset preset)
        {
            switch (preset)
            {
                case UIMeshRigLayoutPreset.Character:
                    return 4;
                case UIMeshRigLayoutPreset.FlagCloth:
                    return 4;
                default:
                    return 1;
            }
        }

        public static UIMeshRigPointLayout GetPoint(UIMeshRigLayoutPreset preset, int index)
        {
            switch (preset)
            {
                case UIMeshRigLayoutPreset.Character:
                    return GetCharacterPoint(index);
                case UIMeshRigLayoutPreset.FlagCloth:
                    return GetFlagPoint(index);
                default:
                    if (index != 0)
                    {
                        throw new System.ArgumentOutOfRangeException(nameof(index));
                    }

                    return new UIMeshRigPointLayout(
                        "Bounce",
                        new Vector2(0.5f, 0.46f),
                        new Vector2(0.18f, 0.18f),
                        new Vector2(0.58f, 0.62f),
                        UIMeshRigMotionPreset.SquashStretch,
                        0f,
                        101);
            }
        }

        private static UIMeshRigPointLayout GetCharacterPoint(int index)
        {
            switch (index)
            {
                case 0:
                    return new UIMeshRigPointLayout(
                        "Root Sway", new Vector2(0.5f, 0.18f), new Vector2(0.12f, 0.1f),
                        new Vector2(0.55f, 0.38f), UIMeshRigMotionPreset.BodySway, 0f, 201);
                case 1:
                    return new UIMeshRigPointLayout(
                        "Torso", new Vector2(0.5f, 0.42f), new Vector2(0.12f, 0.12f),
                        new Vector2(0.38f, 0.32f), UIMeshRigMotionPreset.Breathe, 0.08f, 202);
                case 2:
                    return new UIMeshRigPointLayout(
                        "Chest", new Vector2(0.5f, 0.64f), new Vector2(0.1f, 0.1f),
                        new Vector2(0.32f, 0.27f), UIMeshRigMotionPreset.Breathe, 0.18f, 203);
                case 3:
                    return new UIMeshRigPointLayout(
                        "Head", new Vector2(0.5f, 0.84f), new Vector2(0.09f, 0.09f),
                        new Vector2(0.28f, 0.24f), UIMeshRigMotionPreset.HeadSway, 0.12f, 204);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static UIMeshRigPointLayout GetFlagPoint(int index)
        {
            if (index < 0 || index >= 4)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }

            float x = 0.18f + index * 0.22f;
            return new UIMeshRigPointLayout(
                "Cloth " + (index + 1),
                new Vector2(x, 0.5f),
                new Vector2(0.07f, 0.12f),
                new Vector2(0.28f, 0.58f),
                UIMeshRigMotionPreset.Wave,
                0f,
                301 + index);
        }
    }

    /// <summary>
    /// Clean C# API for building the same layouts exposed by the inspector and creation menu.
    /// </summary>
    public static class UIMeshRigLayoutBuilder
    {
        public static UIMeshRigPoint[] Apply(
            UIMeshRigGraphic rig,
            UIMeshRigLayoutPreset preset,
            bool replaceExisting = true,
            bool previewInEditMode = false)
        {
            return ApplyToOwner(rig, preset, replaceExisting, previewInEditMode);
        }

        public static UIMeshRigPoint[] Apply(
            UIMeshRigWorldRenderer rig,
            UIMeshRigLayoutPreset preset,
            bool replaceExisting = true,
            bool previewInEditMode = false)
        {
            return ApplyToOwner(rig, preset, replaceExisting, previewInEditMode);
        }

        public static UIMeshRigPoint[] Apply(
            UIMeshRigSpriteRenderer rig,
            UIMeshRigLayoutPreset preset,
            bool replaceExisting = true,
            bool previewInEditMode = false)
        {
            return ApplyToOwner(rig, preset, replaceExisting, previewInEditMode);
        }

        public static UIMeshRigPoint CreatePoint(
            UIMeshRigGraphic rig,
            UIMeshRigPointLayout layout,
            bool previewInEditMode = false)
        {
            return CreatePointOnOwner(rig, layout, previewInEditMode);
        }

        public static UIMeshRigPoint CreatePoint(
            UIMeshRigWorldRenderer rig,
            UIMeshRigPointLayout layout,
            bool previewInEditMode = false)
        {
            return CreatePointOnOwner(rig, layout, previewInEditMode);
        }

        public static UIMeshRigPoint CreatePoint(
            UIMeshRigSpriteRenderer rig,
            UIMeshRigPointLayout layout,
            bool previewInEditMode = false)
        {
            return CreatePointOnOwner(rig, layout, previewInEditMode);
        }

        public static void ConfigurePoint(
            UIMeshRigGraphic rig,
            UIMeshRigPoint point,
            UIMeshRigPointLayout layout)
        {
            ConfigurePointOnOwner(rig, point, layout);
        }

        public static void ConfigurePoint(
            UIMeshRigWorldRenderer rig,
            UIMeshRigPoint point,
            UIMeshRigPointLayout layout)
        {
            ConfigurePointOnOwner(rig, point, layout);
        }

        public static void ConfigurePoint(
            UIMeshRigSpriteRenderer rig,
            UIMeshRigPoint point,
            UIMeshRigPointLayout layout)
        {
            ConfigurePointOnOwner(rig, point, layout);
        }

        // WHY: one implementation for every adapter. The typed overloads above are only the public face —
        // the previous shape copied the whole body per renderer, so a fix landed in one copy at a time.
        public static UIMeshRigPoint[] ApplyToOwner(
            IUIMeshRigOwner rig,
            UIMeshRigLayoutPreset preset,
            bool replaceExisting,
            bool previewInEditMode)
        {
            if (rig == null)
            {
                throw new System.ArgumentNullException(nameof(rig));
            }

            if (replaceExisting)
            {
                RemoveExistingPoints(rig);
            }

            int pointCount = UIMeshRigLayoutPresets.GetPointCount(preset);
            UIMeshRigPoint[] result = new UIMeshRigPoint[pointCount];
            if (previewInEditMode && !Application.isPlaying)
            {
                rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            }

            for (int index = 0; index < pointCount; index++)
            {
                UIMeshRigPointLayout layout = UIMeshRigLayoutPresets.GetPoint(preset, index);
                result[index] = CreatePointOnOwner(rig, layout, previewInEditMode);
            }

            rig.NotifyPointChanged();
            return result;
        }

        public static UIMeshRigPoint CreatePointOnOwner(
            IUIMeshRigOwner rig,
            UIMeshRigPointLayout layout,
            bool previewInEditMode)
        {
            if (rig == null)
            {
                throw new System.ArgumentNullException(nameof(rig));
            }

            GameObject pointObject = new GameObject(layout.Name, typeof(RectTransform));
            pointObject.layer = rig.RigTransform.gameObject.layer;
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.SetParent(rig.RigTransform, false);
            ApplyPointTransform(rig, pointRect, layout);

            UIMeshRigPoint point = pointObject.AddComponent<UIMeshRigPoint>();
            ConfigurePointOnOwner(rig, point, layout);
            AddMotion(point, layout, previewInEditMode);
            return point;
        }

        /// <summary>
        /// uGUI points ride the RectTransform anchor so they follow layout resizes; every other adapter
        /// works in the rig's own local units.
        /// </summary>
        public static void ApplyPointTransform(
            IUIMeshRigOwner rig,
            RectTransform pointRect,
            UIMeshRigPointLayout layout)
        {
            if (rig is UIMeshRigGraphic)
            {
                pointRect.anchorMin = layout.CenterNormalized;
                pointRect.anchorMax = layout.CenterNormalized;
                pointRect.sizeDelta = new Vector2(24f, 24f);
                pointRect.anchoredPosition = Vector2.zero;
                return;
            }

            pointRect.localPosition = rig.NormalizedToLocal(layout.CenterNormalized);
            pointRect.sizeDelta = new Vector2(0.12f, 0.12f);
        }

        public static void ConfigurePointOnOwner(
            IUIMeshRigOwner rig,
            UIMeshRigPoint point,
            UIMeshRigPointLayout layout)
        {
            if (rig == null)
            {
                throw new System.ArgumentNullException(nameof(rig));
            }

            if (point == null)
            {
                throw new System.ArgumentNullException(nameof(point));
            }

            point.SetBindingKey(layout.Name);
            point.SetRestCenterNormalized(layout.CenterNormalized);
            point.SetInfluenceRadii(layout.InnerRadiusNormalized, layout.OuterRadiusNormalized);
            point.Strength = layout.Strength;
            point.ApplyFalloffPreset(UIMeshRigFalloffPreset.Smooth);
            point.CaptureRestPose(rig);
        }

        private static void AddMotion(
            UIMeshRigPoint point,
            UIMeshRigPointLayout layout,
            bool previewInEditMode)
        {
            if (layout.MotionPreset == UIMeshRigMotionPreset.Custom)
            {
                return;
            }

            UIMeshRigPointMotion motion = point.gameObject.AddComponent<UIMeshRigPointMotion>();
            motion.ApplyPreset(layout.MotionPreset);
            motion.Phase = layout.Phase;
            motion.Seed = layout.Seed;
            motion.PlayOnEnable = true;
            motion.PreviewInEditMode = previewInEditMode;
        }

        private static void RemoveExistingPoints(IUIMeshRigOwner rig)
        {
            UIMeshRigPoint[] points = rig.RigTransform.GetComponentsInChildren<UIMeshRigPoint>(true);
            for (int index = points.Length - 1; index >= 0; index--)
            {
                if (!UIMeshRigOwnerResolver.OwnsPoint(rig, points[index]))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(points[index].gameObject);
                }
                else
                {
                    Object.DestroyImmediate(points[index].gameObject);
                }
            }

            rig.NotifyPointChanged();
        }
    }
}
