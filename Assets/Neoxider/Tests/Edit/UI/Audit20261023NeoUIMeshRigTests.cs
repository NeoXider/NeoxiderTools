using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoUIMeshRigTests
    {
        [TestCase(128, 64)]
        [TestCase(64, 128)]
        public void CreatePoint_UsesAspectFittedSpriteCoordinates(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f);
            GameObject root = new GameObject("Aspect rig", typeof(RectTransform), typeof(UIMeshRigGraphic));
            try
            {
                RectTransform rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(200f, 200f);
                rect.pivot = new Vector2(0.2f, 0.8f);
                UIMeshRigGraphic rig = root.GetComponent<UIMeshRigGraphic>();
                rig.SetSource(sprite, Color.white);
                UIMeshRigPointLayout layout = new UIMeshRigPointLayout(
                    "Off-center", new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.one,
                    UIMeshRigMotionPreset.Custom, 0f, 0);

                UIMeshRigPoint point = UIMeshRigLayoutBuilder.CreatePoint(rig, layout);

                Assert.That(point.RestCenterNormalized.x, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(point.RestCenterNormalized.y, Is.EqualTo(0.9f).Within(0.0001f));
                Vector2 actualCenter = rig.WorldToNormalized(point.transform.position);
                Assert.That(actualCenter.x, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(actualCenter.y, Is.EqualTo(0.9f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ReparentPoint_UpdatesBothOwnersBelowIntermediateContainers(int adapter)
        {
            GameObject root = new GameObject("Rig ownership audit");
            try
            {
                IUIMeshRigOwner first = CreateOwner(root.transform, adapter);
                IUIMeshRigOwner second = CreateOwner(root.transform, adapter);
                GameObject firstContainer = new GameObject("First container", typeof(RectTransform));
                firstContainer.transform.SetParent(first.RigTransform, false);
                GameObject secondContainer = new GameObject("Second container", typeof(RectTransform));
                secondContainer.transform.SetParent(second.RigTransform, false);
                GameObject pointObject = new GameObject("Point", typeof(RectTransform), typeof(UIMeshRigPoint));
                pointObject.transform.SetParent(firstContainer.transform, false);
                UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
                Assert.That(first.RigPoints.Count, Is.EqualTo(1));
                Assert.That(second.RigPoints.Count, Is.Zero);

                pointObject.transform.SetParent(secondContainer.transform, false);

                Assert.That(first.RigPoints.Count, Is.Zero);
                Assert.That(second.RigPoints.Count, Is.EqualTo(1));
                Assert.That(second.RigPoints[0], Is.SameAs(point));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static IUIMeshRigOwner CreateOwner(Transform parent, int adapter)
        {
            GameObject owner = new GameObject("Owner", typeof(RectTransform));
            owner.transform.SetParent(parent, false);
            switch (adapter)
            {
                case 1:
                    return owner.AddComponent<UIMeshRigWorldRenderer>();
                case 2:
                    return owner.AddComponent<UIMeshRigSpriteRenderer>();
                default:
                    return owner.AddComponent<UIMeshRigGraphic>();
            }
        }
    }
}
