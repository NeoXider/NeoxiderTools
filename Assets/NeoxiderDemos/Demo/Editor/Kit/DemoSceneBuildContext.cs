using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>
    ///     One build pass over an open scene. Builders create everything through this context so a rebuild is
    ///     idempotent: objects are matched by name under their parent (first not-yet-used child wins), reused
    ///     when they exist, created when missing, ordered in call order, and every pre-existing object under a
    ///     generated root that the builders did not touch is removed in <see cref="Finish" />. Objects outside
    ///     generated roots are hand-authored and never pruned (only <see cref="Retire" /> deletes them, on request).
    /// </summary>
    /// <remarks>
    ///     A generated root carries the <see cref="DemoGeneratedRoot" /> marker, or - when <see cref="UseRootMarkers" />
    ///     is off - only the reserved <see cref="RootPrefix" /> name ("[Demo] Key"). Components ensured on generated
    ///     objects get their persistent UnityEvent listeners cleared the first time they are ensured in a build, so
    ///     builders simply re-add their wiring every run. Events of hand-authored components are re-owned with
    ///     <see cref="OwnListeners(Component, UnityEventBase, Object[])" />.
    /// </remarks>
    public sealed class DemoSceneBuildContext
    {
        /// <summary>Name prefix of generated roots ("[Demo] Frame"). Reserved: hand-authored objects must not use it.</summary>
        public const string RootPrefix = "[Demo] ";

        private const int UiLayer = 5;

        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly HashSet<GameObject> _touched = new();
        private readonly HashSet<GameObject> _kept = new();
        private readonly HashSet<Component> _ensured = new();
        private readonly HashSet<GameObject> _roots = new();
        private readonly HashSet<GameObject> _preExisting = new();
        private readonly Dictionary<Transform, int> _nextSibling = new();
        private readonly HashSet<Object> _modified = new();
        private readonly HashSet<Component> _validated = new();
        private readonly HashSet<Component> _runtimeAssetHolders = new();
        private readonly List<DemoTrackedEvent> _trackedEvents = new();
        private readonly List<string> _notes = new();
        private readonly List<string> _warnings = new();
        private Scene _previousActiveScene;
        private bool _begun;

        public DemoSceneBuildContext(Scene scene, string sampleRoot = null)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException("DemoSceneBuildContext needs a valid, loaded scene.", nameof(scene));
            }

            Scene = scene;
            ScenePath = scene.path;
            SampleRoot = sampleRoot;
        }

        public Scene Scene { get; }

        /// <summary>Asset path of the scene (empty for an unsaved scene).</summary>
        public string ScenePath { get; }

        /// <summary>Asset path of the sample root the scene belongs to (null when built outside the runner).</summary>
        public string SampleRoot { get; }

        /// <summary>Id of the builder currently running (stamped on the roots it creates).</summary>
        public string CurrentBuilder { get; set; } = string.Empty;

        /// <summary>
        ///     True (default): generated roots carry the <see cref="DemoGeneratedRoot" /> marker. False: roots are
        ///     plain objects named "[Demo] key", recognised by that reserved name only, so a scene of a sample that
        ///     must not depend on the Demo sample assembly (NeoxiderPages) gets no missing scripts. The runner turns
        ///     it off for <see cref="DemoSample.NeoxiderPages" /> builders; set it before the first root.
        /// </summary>
        public bool UseRootMarkers { get; set; } = true;

        public int Created { get; private set; }
        public int Reused { get; private set; }
        public int Removed { get; private set; }
        public int Adopted { get; private set; }
        public bool IsBuilding => _begun && !IsFinished;
        public bool IsFinished { get; private set; }
        public IReadOnlyList<string> Notes => _notes;
        public IReadOnlyList<string> Warnings => _warnings;

        internal IEnumerable<GameObject> TouchedRoots => _roots;
        internal IEnumerable<Component> ValidatedComponents => _validated;
        internal IReadOnlyList<DemoTrackedEvent> TrackedEvents => _trackedEvents;

        /// <summary>Starts the pass: snapshots generated objects and makes the scene active for new objects.</summary>
        public void Begin()
        {
            if (_begun)
            {
                throw new InvalidOperationException("DemoSceneBuildContext.Begin was already called.");
            }

            _begun = true;
            _previousActiveScene = SceneManager.GetActiveScene();
            if (_previousActiveScene != Scene)
            {
                SceneManager.SetActiveScene(Scene);
            }

            foreach (GameObject root in FindGeneratedRoots())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    _preExisting.Add(node.gameObject);
                }
            }
        }

        /// <summary>
        ///     Ends the pass: removes stale generated roots and objects, marks touched objects dirty (recording
        ///     prefab-instance overrides) and restores the previously active scene. Does not save.
        /// </summary>
        public void Finish()
        {
            RequireBuilding();
            try
            {
                foreach (GameObject root in FindGeneratedRoots())
                {
                    if (root != null && !_roots.Contains(root))
                    {
                        _notes.Add("Removed stale generated root '" + PathOf(root.transform) + "'.");
                        Removed += root.GetComponentsInChildren<Transform>(true).Length;
                        Object.DestroyImmediate(root);
                    }
                }

                foreach (GameObject root in _roots)
                {
                    if (root != null)
                    {
                        PruneChildren(root.transform);
                    }
                }

                foreach (GameObject touched in _touched)
                {
                    MarkDirty(touched);
                }

                foreach (GameObject kept in _kept)
                {
                    MarkDirty(kept);
                }

                foreach (Component component in _ensured)
                {
                    MarkDirty(component);
                }

                foreach (Object modified in _modified)
                {
                    MarkDirty(modified);
                }

                EditorSceneManager.MarkSceneDirty(Scene);
            }
            finally
            {
                IsFinished = true;
                if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded &&
                    _previousActiveScene != SceneManager.GetActiveScene())
                {
                    SceneManager.SetActiveScene(_previousActiveScene);
                }
            }
        }

        /// <summary>Top-level generated root "[Demo] key" (created when missing, reused otherwise).</summary>
        /// <param name="key">Stable key, unique in the scene (e.g. "Frame", "World", "Services").</param>
        /// <param name="ui">Give the root a RectTransform and the UI layer (canvases).</param>
        public GameObject Root(string key, bool ui = false)
        {
            return EnsureRoot(null, key, ui);
        }

        /// <summary>Generated root under a hand-authored parent (for generated content inside authored objects).</summary>
        public GameObject RootUnder(Transform parent, string key, bool ui = false)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "RootUnder needs a hand-authored parent; use Root for top-level roots.");
            }

            if (parent.gameObject.scene != Scene)
            {
                throw new ArgumentException("RootUnder parent '" + PathOf(parent) + "' is not in scene " + Scene.name + ".");
            }

            if (IsGenerated(parent.gameObject))
            {
                throw new ArgumentException("RootUnder parent '" + PathOf(parent) +
                                            "' is already inside a generated root; use Child/UiChild instead.");
            }

            return EnsureRoot(parent, key, ui);
        }

        /// <summary>Plain child object (Transform) under a parent created or ensured in this build.</summary>
        public GameObject Child(Transform parent, string name)
        {
            return EnsureChild(parent, name, false);
        }

        /// <summary>UI child object (RectTransform) under a parent created or ensured in this build.</summary>
        public RectTransform UiChild(Transform parent, string name)
        {
            return (RectTransform)EnsureChild(parent, name, true).transform;
        }

        /// <summary>
        ///     Gets or adds a component. The first use in a build clears its persistent listeners on generated
        ///     objects, and serialized UnityEvent fields that are still null (fields without an initializer on a
        ///     freshly added component) get an empty event, so <see cref="DemoWiring" /> can wire them right away.
        /// </summary>
        public T Ensure<T>(GameObject target) where T : Component
        {
            return (T)Ensure(target, typeof(T));
        }

        /// <summary>Non-generic <see cref="Ensure{T}" />.</summary>
        public Component Ensure(GameObject target, Type componentType)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType) || componentType.IsAbstract)
            {
                throw new ArgumentException("Ensure needs a concrete Component type, got " + componentType + ".");
            }

            Component component = target.GetComponent(componentType);
            if (component == null)
            {
                ResolveConflict(target, componentType);
                component = target.AddComponent(componentType);
                if (component == null)
                {
                    throw new InvalidOperationException("Cannot add " + componentType.Name + " to '" + PathOf(target.transform) +
                                                        "'. A conflicting component is present; give the element its own child name.");
                }
            }

            if (_ensured.Add(component))
            {
                if (InitializeNullEvents(component))
                {
                    _modified.Add(component);
                }

                if (IsGenerated(target) && !PrefabUtility.IsPartOfPrefabInstance(component))
                {
                    ClearPersistentCalls(component);
                }
            }

            return component;
        }

        /// <summary>
        ///     Returns the first unused child named <paramref name="name" /> (touched, ordered, subtree kept) or null.
        ///     Use it with <see cref="Attach" /> for objects created by factories (TMP_DefaultControls, prefabs).
        /// </summary>
        public GameObject Reuse(Transform parent, string name)
        {
            RequireTouchedParent(parent);
            Transform found = FindUnusedChild(parent, name);
            if (found == null)
            {
                return null;
            }

            Reused++;
            PlaceInOrder(parent, found);
            Keep(found.gameObject);
            return found.gameObject;
        }

        /// <summary>Parents an object created outside the context, names it and keeps its whole subtree.</summary>
        public GameObject Attach(GameObject created, Transform parent, string name)
        {
            if (created == null)
            {
                throw new ArgumentNullException(nameof(created));
            }

            RequireTouchedParent(parent);
            created.name = name;
            created.transform.SetParent(parent, false);
            if (parent is RectTransform)
            {
                foreach (Transform node in created.GetComponentsInChildren<Transform>(true))
                {
                    node.gameObject.layer = parent.gameObject.layer;
                }
            }

            Created++;
            PlaceInOrder(parent, created.transform);
            Keep(created);
            return created;
        }

        /// <summary>
        ///     Marks a whole subtree as produced by this build: nothing under it is pruned. The root counts as used;
        ///     its descendants stay matchable, so a later <see cref="Child" /> / <see cref="UiChild" /> under a reused
        ///     primitive or prefab finds the existing child instead of creating a duplicate.
        /// </summary>
        public void Keep(GameObject subtreeRoot)
        {
            RequireBuilding();
            if (subtreeRoot == null)
            {
                return;
            }

            _touched.Add(subtreeRoot);
            foreach (Transform node in subtreeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!_touched.Contains(node.gameObject))
                {
                    _kept.Add(node.gameObject);
                }
            }
        }

        /// <summary>Prefab instance child (reused when it already instantiates the same prefab).</summary>
        public GameObject Prefab(Transform parent, string name, GameObject prefabAsset)
        {
            if (prefabAsset == null || !EditorUtility.IsPersistent(prefabAsset))
            {
                throw new ArgumentException("Prefab needs a prefab asset for '" + name + "'.", nameof(prefabAsset));
            }

            RequireTouchedParent(parent);
            Transform existing = FindUnusedChild(parent, name);
            if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefabAsset)
            {
                return Reuse(parent, name);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, Scene);
            return Attach(instance, parent, name);
        }

        /// <summary>Primitive mesh child (cube, sphere...) with its default collider and renderer.</summary>
        public GameObject Primitive(Transform parent, string name, PrimitiveType type)
        {
            GameObject existing = Reuse(parent, name);
            return existing != null ? existing : Attach(GameObject.CreatePrimitive(type), parent, name);
        }

        /// <summary>First component of type <typeparamref name="T" /> in the scene (hierarchy-path order).</summary>
        /// <param name="includeGenerated">Also search inside generated roots.</param>
        public T Find<T>(bool includeGenerated = false) where T : Component
        {
            List<T> all = FindAll<T>(includeGenerated);
            if (all.Count > 1)
            {
                _notes.Add("Several " + typeof(T).Name + " in the scene; using '" + PathOf(all[0].transform) + "'.");
            }

            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>Every component of type <typeparamref name="T" /> in the scene, sorted by hierarchy path.</summary>
        public List<T> FindAll<T>(bool includeGenerated = false) where T : Component
        {
            List<T> result = new();
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (includeGenerated || !IsGenerated(component.gameObject))
                    {
                        result.Add(component);
                    }
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform), PathOf(b.transform)));
            return result;
        }

        /// <summary>
        ///     Hand-authored component when the scene has one; otherwise ensures it on
        ///     "[Demo] <paramref name="rootKey" />/<paramref name="objectName" />".
        /// </summary>
        public T FindOrCreate<T>(string rootKey, string objectName) where T : Component
        {
            T existing = Find<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject host = Child(Root(rootKey).transform, objectName);
            return Ensure<T>(host);
        }

        /// <summary>Top-level hand-authored object named <paramref name="name" />, or null.</summary>
        public GameObject FindHandAuthoredRoot(string name)
        {
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                if (root.name == name && !IsGenerated(root))
                {
                    return root;
                }
            }

            return null;
        }

        /// <summary>
        ///     Deletes a hand-authored object on purpose (a one-time migration of a pre-kit layout, a bootstrap
        ///     leftover): counted in <see cref="Removed" /> and noted in the scene summary. Check the object's known
        ///     shape before calling it. Generated objects and subtrees holding objects of this pass are refused.
        /// </summary>
        /// <returns>False when <paramref name="handAuthored" /> is null (already gone).</returns>
        public bool Retire(GameObject handAuthored, string reason)
        {
            RequireBuilding();
            if (handAuthored == null)
            {
                return false;
            }

            string path = PathOf(handAuthored.transform);
            if (handAuthored.scene != Scene)
            {
                throw new ArgumentException("Retire: '" + path + "' is not in scene " + Scene.name + ".");
            }

            if (IsGenerated(handAuthored))
            {
                throw new InvalidOperationException("Retire: '" + path +
                                                    "' is generated; the context prunes generated objects itself.");
            }

            if (PrefabUtility.IsPartOfPrefabInstance(handAuthored) && !PrefabUtility.IsOutermostPrefabInstanceRoot(handAuthored))
            {
                throw new InvalidOperationException("Retire: '" + path + "' is inside a prefab instance; retire the " +
                                                    "instance root or change the prefab.");
            }

            Transform[] subtree = handAuthored.GetComponentsInChildren<Transform>(true);
            foreach (Transform node in subtree)
            {
                if (_touched.Contains(node.gameObject) || _kept.Contains(node.gameObject))
                {
                    throw new InvalidOperationException("Retire: '" + path + "' holds '" + PathOf(node) +
                                                        "', which this pass produced; retire it before building there.");
                }
            }

            Removed += subtree.Length;
            _notes.Add("Retired hand-authored '" + path + "' (" + (string.IsNullOrEmpty(reason) ? "no reason given" : reason) +
                       ").");
            Object.DestroyImmediate(handAuthored);
            return true;
        }

        /// <summary>
        ///     Re-owns the persistent listeners a builder adds to an event of a hand-authored component: removes
        ///     calls whose target is missing, lives under a generated root or is one of <paramref name="alsoOwned" />
        ///     (see <see cref="DemoWiring.PruneOwned(UnityEventBase, Object[])" />), and registers the event so the
        ///     runner validates its calls after the build. Add the builder's wiring again afterwards.
        /// </summary>
        /// <returns>Number of removed calls.</returns>
        public int OwnListeners(Component owner, UnityEventBase unityEvent, params Object[] alsoOwned)
        {
            TrackListeners(owner, unityEvent);
            return DemoWiring.PruneOwned(unityEvent, alsoOwned);
        }

        /// <summary><see cref="OwnListeners(Component, UnityEventBase, Object[])" /> with a predicate for extra owned targets.</summary>
        public int OwnListeners(Component owner, UnityEventBase unityEvent, Func<Object, bool> alsoOwned)
        {
            TrackListeners(owner, unityEvent);
            return DemoWiring.PruneOwned(unityEvent, alsoOwned);
        }

        /// <summary>
        ///     Registers an event of a hand-authored component for the runner's validation (calls without a target or
        ///     with an unknown method fail the build) without pruning it. Events on generated objects are always
        ///     validated.
        /// </summary>
        public void TrackListeners(Component owner, UnityEventBase unityEvent)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "TrackListeners needs the component that owns the event.");
            }

            if (unityEvent == null)
            {
                throw new ArgumentNullException(nameof(unityEvent), "TrackListeners: the UnityEvent on " + owner.GetType().Name +
                                                                    " '" + PathOf(owner.transform) + "' is null.");
            }

            foreach (DemoTrackedEvent tracked in _trackedEvents)
            {
                if (ReferenceEquals(tracked.Event, unityEvent))
                {
                    return;
                }
            }

            _trackedEvents.Add(new DemoTrackedEvent(owner, unityEvent));
            _modified.Add(owner);
        }

        /// <summary>Validates every persistent listener of a hand-authored component after the build (like generated ones).</summary>
        public void ValidateListeners(Component component)
        {
            if (component != null)
            {
                _validated.Add(component);
            }
        }

        /// <summary>
        ///     Lets <paramref name="holder" /> keep a runtime-generated sprite or material: an [ExecuteAlways] adapter
        ///     re-creates it when the scene opens, so validation does not reject it. The reference is not saved;
        ///     prefer pointing the holder at a persistent asset when one exists.
        /// </summary>
        public void AllowRuntimeAsset(Component holder, string reason)
        {
            if (holder == null)
            {
                throw new ArgumentNullException(nameof(holder));
            }

            if (_runtimeAssetHolders.Add(holder))
            {
                _notes.Add("Runtime assets allowed on " + holder.GetType().Name + " '" + PathOf(holder.transform) + "' (" +
                           (string.IsNullOrEmpty(reason) ? "adapter-owned" : reason) + ").");
            }
        }

        /// <summary>True when <see cref="AllowRuntimeAsset" /> was called for <paramref name="holder" />.</summary>
        public bool IsRuntimeAssetAllowed(Component holder)
        {
            return holder != null && _runtimeAssetHolders.Contains(holder);
        }

        /// <summary>True when the object lives under a generated root (marker or reserved "[Demo] " name).</summary>
        public static bool IsGenerated(GameObject target)
        {
            for (Transform node = target != null ? target.transform : null; node != null; node = node.parent)
            {
                if (IsGeneratedRoot(node.gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the target of a persistent call (component or GameObject) lives under a generated root.</summary>
        public static bool IsGeneratedTarget(Object target)
        {
            GameObject host = HostOf(target);
            return host != null && IsGenerated(host);
        }

        /// <summary>GameObject of a component or GameObject target; null for assets, missing or destroyed targets.</summary>
        public static GameObject HostOf(Object target)
        {
            // WHY: a destroyed target still matches 'is Component'; only Unity's null check sees it, and touching
            // .gameObject on it throws.
            if (target == null)
            {
                return null;
            }

            return target switch
            {
                Component component => component.gameObject,
                GameObject gameObject => gameObject,
                _ => null
            };
        }

        /// <summary>True for a generated root itself: a <see cref="DemoGeneratedRoot" /> marker or a "[Demo] " name.</summary>
        public static bool IsGeneratedRoot(GameObject target)
        {
            return target != null && (target.GetComponent<DemoGeneratedRoot>() != null ||
                                      target.name.StartsWith(RootPrefix, StringComparison.Ordinal));
        }

        /// <summary>Key of a generated root (marker key, or the name without the "[Demo] " prefix).</summary>
        public static bool TryGetRootKey(GameObject root, out string key)
        {
            key = null;
            if (root == null)
            {
                return false;
            }

            DemoGeneratedRoot marker = root.GetComponent<DemoGeneratedRoot>();
            if (marker != null)
            {
                key = marker.Key;
                return true;
            }

            if (root.name.StartsWith(RootPrefix, StringComparison.Ordinal))
            {
                key = root.name.Substring(RootPrefix.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Sets a serialized field by name through SerializedObject (records prefab overrides, marks dirty).
        ///     Supports Object references (a GameObject resolves to its matching component), numbers, bool, string,
        ///     enums, colors, vectors, rects, layer masks, curves and arrays/lists of those. Nested paths work too:
        ///     "_binding._memberName", "_maps.Array.size", "_maps.Array.data[2]._id".
        /// </summary>
        public void Set(Object target, string propertyPath, object value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Set '" + propertyPath + "' on a null target.");
            }

            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                throw new ArgumentException(target.GetType().FullName + " on '" + Describe(target) +
                                            "' has no serialized field '" + propertyPath + "'. Serialized fields: " +
                                            ListFields(serialized) + ".");
            }

            Assign(property, value, target);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _modified.Add(target);
        }

        /// <summary>Records an object changed through its C# API so it is saved (and prefab overrides recorded).</summary>
        public void MarkModified(Object target)
        {
            if (target != null)
            {
                _modified.Add(target);
            }
        }

        public void Note(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _notes.Add(message);
            }
        }

        public void Warn(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _warnings.Add(message);
            }
        }

        /// <summary>Hierarchy path of a transform ("[Demo] Frame/Content/Btn_Save").</summary>
        public static string PathOf(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            StringBuilder builder = new(target.name);
            Transform parent = target.parent;
            while (parent != null)
            {
                builder.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return builder.ToString();
        }

        /// <summary>Every generated root of the scene (outermost only), in hierarchy order.</summary>
        internal List<GameObject> FindGeneratedRoots()
        {
            List<GameObject> roots = new();
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                CollectGeneratedRoots(root.transform, roots);
            }

            return roots;
        }

        private static void CollectGeneratedRoots(Transform node, List<GameObject> roots)
        {
            if (IsGeneratedRoot(node.gameObject))
            {
                roots.Add(node.gameObject);
                return;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CollectGeneratedRoots(node.GetChild(i), roots);
            }
        }

        private GameObject FindRootObject(Transform parent, string key)
        {
            string plainName = RootPrefix + key;
            foreach (GameObject candidate in FindGeneratedRoots())
            {
                if (candidate.transform.parent != parent)
                {
                    continue;
                }

                DemoGeneratedRoot marker = candidate.GetComponent<DemoGeneratedRoot>();
                if (marker != null ? marker.Key == key : candidate.name == plainName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private GameObject EnsureRoot(Transform parent, string key, bool ui)
        {
            RequireBuilding();
            if (string.IsNullOrWhiteSpace(key) || key.IndexOf('/') >= 0)
            {
                throw new ArgumentException("Generated root keys must be non-empty and contain no '/': '" + key + "'.");
            }

            string name = parent == null || !UseRootMarkers ? RootPrefix + key : key;
            GameObject root = FindRootObject(parent, key);
            if (root == null)
            {
                root = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                if (parent != null)
                {
                    root.transform.SetParent(parent, false);
                }
                else if (root.scene != Scene)
                {
                    SceneManager.MoveGameObjectToScene(root, Scene);
                }

                Created++;
            }
            else if (!_roots.Contains(root))
            {
                Reused++;
            }

            DemoGeneratedRoot marker = root.GetComponent<DemoGeneratedRoot>();
            if (UseRootMarkers)
            {
                if (marker == null)
                {
                    marker = root.AddComponent<DemoGeneratedRoot>();
                }

                marker.Stamp(key, CurrentBuilder);
                _ensured.Add(marker);
            }
            else if (marker != null)
            {
                _notes.Add("Removed the DemoGeneratedRoot marker from '" + PathOf(root.transform) +
                           "' (this scene uses marker-less roots).");
                Object.DestroyImmediate(marker);
            }

            if (ui && !(root.transform is RectTransform))
            {
                root.AddComponent<RectTransform>();
            }

            if (ui)
            {
                root.layer = UiLayer;
            }

            root.name = name;
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            _roots.Add(root);
            _touched.Add(root);
            return root;
        }

        private GameObject EnsureChild(Transform parent, string name, bool ui)
        {
            RequireTouchedParent(parent);
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Child objects need a name (it is how a rebuild finds them again).");
            }

            Transform found = FindUnusedChild(parent, name);
            if (found == null)
            {
                GameObject created = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                created.layer = parent.gameObject.layer;
                created.transform.SetParent(parent, false);
                found = created.transform;
                Created++;
            }
            else
            {
                Reused++;
                if (ui && !(found is RectTransform))
                {
                    GameObject host = found.gameObject;
                    host.AddComponent<RectTransform>();
                    found = host.transform;
                }
            }

            PlaceInOrder(parent, found);
            _touched.Add(found.gameObject);
            if (!found.gameObject.activeSelf)
            {
                found.gameObject.SetActive(true);
            }

            return found.gameObject;
        }

        private Transform FindUnusedChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name && !_touched.Contains(child.gameObject))
                {
                    return child;
                }
            }

            return null;
        }

        private void PlaceInOrder(Transform parent, Transform child)
        {
            int index = _nextSibling.TryGetValue(parent, out int next) ? next : 0;
            _nextSibling[parent] = index + 1;
            if (index < parent.childCount && child.GetSiblingIndex() != index)
            {
                child.SetSiblingIndex(index);
            }
        }

        private bool IsProduced(GameObject node)
        {
            return _touched.Contains(node) || _kept.Contains(node);
        }

        private void PruneChildren(Transform parent)
        {
            // WHY: children of prefab instances belong to the prefab asset and cannot be deleted from the scene.
            if (PrefabUtility.IsPartOfPrefabInstance(parent.gameObject))
            {
                return;
            }

            List<GameObject> stale = new();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                GameObject node = child.gameObject;
                if (IsTransient(node))
                {
                    continue;
                }

                if (IsProduced(node))
                {
                    if (!PrefabUtility.IsPartOfPrefabInstance(node))
                    {
                        PruneChildren(child);
                    }

                    continue;
                }

                if (_preExisting.Contains(node))
                {
                    stale.Add(node);
                    continue;
                }

                Adopted++;
                foreach (Transform kept in node.GetComponentsInChildren<Transform>(true))
                {
                    _kept.Add(kept.gameObject);
                }

                _warnings.Add("'" + PathOf(child) + "' was created under a generated root without the build context " +
                              "(ctx.Child / UiChild / Attach); it was kept, but a rebuild will replace it.");
            }

            foreach (GameObject node in stale)
            {
                Removed += node.GetComponentsInChildren<Transform>(true).Length;
                Object.DestroyImmediate(node);
            }
        }

        private static bool IsTransient(GameObject node)
        {
            // WHY: TMP creates sub-mesh children for fallback glyphs on its own; they are not builder output.
            return (node.hideFlags & HideFlags.DontSave) != 0 || node.GetComponent<TMP_SubMeshUI>() != null ||
                   node.GetComponent<TMP_SubMesh>() != null;
        }

        private void ResolveConflict(GameObject target, Type componentType)
        {
            Type exclusiveBase = null;
            for (Type type = componentType; type != null && type != typeof(MonoBehaviour) && type != typeof(Component);
                 type = type.BaseType)
            {
                if (type.IsDefined(typeof(DisallowMultipleComponent), false))
                {
                    exclusiveBase = type;
                }
            }

            if (exclusiveBase == null)
            {
                return;
            }

            Component conflicting = target.GetComponent(exclusiveBase);
            if (conflicting == null || conflicting.GetType() == componentType)
            {
                return;
            }

            if (!IsGenerated(target) || PrefabUtility.IsPartOfPrefabInstance(conflicting))
            {
                throw new InvalidOperationException("'" + PathOf(target.transform) + "' already has " +
                                                    conflicting.GetType().Name + ", which excludes " + componentType.Name +
                                                    ". Use a separate child for it.");
            }

            _notes.Add("Replaced " + conflicting.GetType().Name + " with " + componentType.Name + " on '" +
                       PathOf(target.transform) + "'.");
            _ensured.Remove(conflicting);
            Object.DestroyImmediate(conflicting);
        }

        /// <summary>Gives null serialized UnityEvent fields of a MonoBehaviour an empty event; true when one was set.</summary>
        private static bool InitializeNullEvents(Component component)
        {
            if (!(component is MonoBehaviour))
            {
                return false;
            }

            bool changed = false;
            for (Type type = component.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(InstanceFields))
                {
                    if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType) || field.FieldType.IsAbstract ||
                        field.IsInitOnly || field.IsNotSerialized ||
                        field.IsDefined(typeof(SerializeReference), false) ||
                        (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false)) ||
                        field.FieldType.GetConstructor(Type.EmptyTypes) == null ||
                        field.GetValue(component) != null)
                    {
                        continue;
                    }

                    field.SetValue(component, Activator.CreateInstance(field.FieldType));
                    changed = true;
                }
            }

            return changed;
        }

        private static void ClearPersistentCalls(Component component)
        {
            SerializedObject serialized = new(component);
            SerializedProperty iterator = serialized.GetIterator();
            bool changed = false;
            bool enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                enterChildren = iterator.propertyType == SerializedPropertyType.Generic;
                if (!enterChildren || iterator.name != "m_PersistentCalls")
                {
                    continue;
                }

                SerializedProperty calls = iterator.FindPropertyRelative("m_Calls");
                if (calls != null && calls.isArray && calls.arraySize > 0)
                {
                    calls.ClearArray();
                    changed = true;
                }

                enterChildren = false;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void MarkDirty(Object target)
        {
            if (target == null)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private void RequireBuilding()
        {
            if (!_begun)
            {
                throw new InvalidOperationException("Call DemoSceneBuildContext.Begin before building (the runner does this).");
            }

            if (IsFinished)
            {
                throw new InvalidOperationException("This DemoSceneBuildContext is already finished.");
            }
        }

        private void RequireTouchedParent(Transform parent)
        {
            RequireBuilding();
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (!IsProduced(parent.gameObject))
            {
                throw new InvalidOperationException("Parent '" + PathOf(parent) + "' was not produced in this build. " +
                                                    "Take parents from ctx.Root / RootUnder / Child / UiChild (or the kit factories).");
            }
        }

        private static void Assign(SerializedProperty property, object value, Object owner)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    AssignObject(property, value, owner);
                    return;
                case SerializedPropertyType.Integer:
                    property.longValue = Convert.ToInt64(value);
                    return;
                case SerializedPropertyType.ArraySize:
                    property.intValue = Convert.ToInt32(value);
                    return;
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(value);
                    return;
                case SerializedPropertyType.Float:
                    property.doubleValue = Convert.ToDouble(value);
                    return;
                case SerializedPropertyType.String:
                    property.stringValue = value == null ? string.Empty : value.ToString();
                    return;
                case SerializedPropertyType.Enum:
                    property.intValue = Convert.ToInt32(value);
                    return;
                case SerializedPropertyType.Color:
                    property.colorValue = value is Color32 color32 ? color32 : (Color)value;
                    return;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)value;
                    return;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)value;
                    return;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = (Vector4)value;
                    return;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = (Vector2Int)value;
                    return;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = (Vector3Int)value;
                    return;
                case SerializedPropertyType.Rect:
                    property.rectValue = (Rect)value;
                    return;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = (Quaternion)value;
                    return;
                case SerializedPropertyType.LayerMask:
                    property.intValue = value is LayerMask mask ? mask.value : Convert.ToInt32(value);
                    return;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = (AnimationCurve)value;
                    return;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = value;
                    return;
            }

            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                if (!(value is IList list))
                {
                    throw new ArgumentException("Field '" + property.propertyPath + "' is an array/list; pass an IList.");
                }

                property.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    Assign(property.GetArrayElementAtIndex(i), list[i], owner);
                }

                return;
            }

            throw new NotSupportedException("Field '" + property.propertyPath + "' (" + property.propertyType + ", " +
                                            property.type + ") is not supported by ctx.Set; set its members by path " +
                                            "('" + property.propertyPath + "._member', '.Array.size', '.Array.data[i]') " +
                                            "or through SerializedObject and call ctx.MarkModified.");
        }

        private static void AssignObject(SerializedProperty property, object value, Object owner)
        {
            if (value == null)
            {
                property.objectReferenceValue = null;
                return;
            }

            if (!(value is Object reference))
            {
                throw new ArgumentException("Field '" + property.propertyPath + "' holds a Unity object, got " +
                                            value.GetType().Name + ".");
            }

            property.objectReferenceValue = reference;
            if (property.objectReferenceValue == reference)
            {
                return;
            }

            if (reference is GameObject host)
            {
                foreach (Component candidate in host.GetComponents<Component>())
                {
                    property.objectReferenceValue = candidate;
                    if (property.objectReferenceValue == candidate)
                    {
                        return;
                    }
                }
            }

            property.objectReferenceValue = null;
            throw new ArgumentException("Field '" + property.propertyPath + "' (" + property.type + ") on " +
                                        owner.GetType().Name + " cannot hold " + reference.GetType().Name + " '" +
                                        reference.name + "'.");
        }

        private static string ListFields(SerializedObject serialized)
        {
            List<string> names = new();
            SerializedProperty iterator = serialized.GetIterator();
            bool enter = true;
            while (iterator.NextVisible(enter))
            {
                enter = false;
                if (iterator.name != "m_Script")
                {
                    names.Add(iterator.name);
                }
            }

            return names.Count == 0 ? "(none)" : string.Join(", ", names);
        }

        private static string Describe(Object target)
        {
            return target is Component component ? PathOf(component.transform) : target.name;
        }
    }

    /// <summary>An event of a hand-authored component registered with <see cref="DemoSceneBuildContext.TrackListeners" />.</summary>
    internal readonly struct DemoTrackedEvent
    {
        public DemoTrackedEvent(Component owner, UnityEventBase unityEvent)
        {
            Owner = owner;
            Event = unityEvent;
        }

        public Component Owner { get; }
        public UnityEventBase Event { get; }
    }
}
