using System.Collections;
using System.Collections.Generic;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PageManager = Neo.UI.UI;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoUILifecycleTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in _roots)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
        }

        [UnityTest]
        public IEnumerator VisualToggle_AwakeResolvesToggleAndInitializesValueWithoutStartupEvents()
        {
            GameObject root = Track(new GameObject("Toggle initialization", typeof(RectTransform)));
            root.SetActive(false);
            Toggle input = root.AddComponent<Toggle>();
            input.isOn = true;
            VisualToggle visual = root.AddComponent<VisualToggle>();
            JsonUtility.FromJsonOverwrite("{\"_setOnAwake\":false}", visual);
            visual.On = new UnityEvent();
            int events = 0;
            visual.On.AddListener(() => events++);
            visual.Value.AddListener(_ => events++);

            root.SetActive(true);
            yield return null;

            Assert.That(visual.IsActive, Is.True);
            Assert.That(visual.ValueBool, Is.True);
            Assert.That(events, Is.Zero);

            input.isOn = false;
            Assert.That(visual.IsActive, Is.False);
            Assert.That(visual.ValueBool, Is.False);
            Assert.That(events, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator VisualToggle_StandaloneSerializedStateInitializesReactiveValue()
        {
            GameObject root = Track(new GameObject("Standalone toggle initialization"));
            root.SetActive(false);
            VisualToggle visual = root.AddComponent<VisualToggle>();
            JsonUtility.FromJsonOverwrite("{\"_isActive\":true,\"_setOnAwake\":false}", visual);

            root.SetActive(true);
            yield return null;

            Assert.That(visual.IsActive, Is.True);
            Assert.That(visual.ValueBool, Is.True);
        }

        [UnityTest]
        public IEnumerator VisualToggle_ToggleGroupRejectsOff_UsesActualToggleState()
        {
            ToggleGroup group = Track(new GameObject("Required selection")).AddComponent<ToggleGroup>();
            group.allowSwitchOff = false;
            GameObject root = Track(new GameObject("Grouped toggle", typeof(RectTransform)));
            root.SetActive(false);
            Toggle input = root.AddComponent<Toggle>();
            input.group = group;
            input.isOn = true;
            VisualToggle visual = root.AddComponent<VisualToggle>();
            root.SetActive(true);
            yield return null;

            visual.SetActive(false);

            Assert.That(input.isOn, Is.True);
            Assert.That(visual.IsActive, Is.True);
            Assert.That(visual.ValueBool, Is.True);
        }

        [UnityTest]
        public IEnumerator ButtonChangePage_RespectsCanvasGroupInteractability()
        {
            GameObject root = Track(new GameObject("Grouped button", typeof(RectTransform), typeof(CanvasGroup),
                typeof(Button)));
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            Button input = root.GetComponent<Button>();
            ButtonChangePage button = root.AddComponent<ButtonChangePage>();
            button.OnClick = new UnityEvent();
            int clicks = 0;
            button.OnClick.AddListener(() => clicks++);
            group.interactable = false;
            yield return null;

            Assert.That(input.IsInteractable(), Is.False);
            Assert.That(button.Interactable, Is.False);
            button.ExecuteClick();
            Assert.That(clicks, Is.Zero);

            group.interactable = true;
            yield return null;
            button.ExecuteClick();
            Assert.That(button.Interactable, Is.True);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ButtonShake_EnableStopAndReenablePreserveLocalDepth()
        {
            GameObject root = Track(new GameObject("Shake depth", typeof(RectTransform)));
            root.SetActive(false);
            Vector3 original = new Vector3(20f, 30f, 17f);
            root.transform.localPosition = original;
            ButtonShake shake = root.AddComponent<ButtonShake>();

            root.SetActive(true);
            Assert.That(root.transform.localPosition, Is.EqualTo(original));
            shake.Shake();
            shake.StopShake();
            Assert.That(root.transform.localPosition, Is.EqualTo(original));
            root.SetActive(false);
            root.SetActive(true);
            yield return null;

            Assert.That(root.transform.localPosition, Is.EqualTo(original));
        }

        [UnityTest]
        public IEnumerator PageManager_EmptyConfigurationStillPublishesSelectionAndClearsStaticOwner()
        {
            PageManager first = Track(new GameObject("First page manager")).AddComponent<PageManager>();
            first.OnChangePage = new UnityEvent<int>();
            int observed = int.MinValue;
            first.OnChangePage.AddListener(index => observed = index);
            first.SetPage(-1);
            first.SetCurrentPage(false);
            Assert.That(first.id, Is.EqualTo(-1));
            Assert.That(observed, Is.EqualTo(-1));
            Assert.That(PageManager.I, Is.SameAs(first));

            PageManager second = Track(new GameObject("Second page manager")).AddComponent<PageManager>();
            Object.Destroy(first.gameObject);
            yield return null;
            Assert.That(PageManager.I, Is.SameAs(second));

            Object.Destroy(second.gameObject);
            yield return null;
            Assert.That(ReferenceEquals(PageManager.I, null), Is.True);
        }

        [UnityTest]
        public IEnumerator PageManager_DestroyedCachedPage_StillPublishesSelection()
        {
            GameObject root = Track(new GameObject("Cached page manager"));
            root.SetActive(false);
            PageManager manager = root.AddComponent<PageManager>();
            GameObject page = Track(new GameObject("Temporary page"));
            page.SetActive(false);
            PageConfiguration configuration = new PageConfiguration { _pages = new[] { page } };
            // Round-trip serialized scene references in the same process before Awake runs.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(configuration), manager);
            root.SetActive(true);
            Assert.That(page.activeSelf, Is.True);
            Object.Destroy(page);
            yield return null;
            manager.OnChangePage = new UnityEvent<int>();
            int observed = -1;
            manager.OnChangePage.AddListener(index => observed = index);

            manager.SetPage(0);

            Assert.That(manager.id, Is.Zero);
            Assert.That(observed, Is.Zero);
        }

        private GameObject Track(GameObject root)
        {
            _roots.Add(root);
            return root;
        }

        [System.Serializable]
        private sealed class PageConfiguration
        {
            public GameObject[] _pages;
        }
    }
}
