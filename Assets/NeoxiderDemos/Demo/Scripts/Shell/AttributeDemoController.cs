using System.Collections.Generic;
using System.Text;
using Neo.Demo;
using Neo.Tools;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Editor</b> inspector-attribute demo (Scenes/Tools/AttributeExample.unity).
    ///     The AttributeExampleBuilder saves the UI, the stage and every persistent listener; this component only
    ///     reads the <see cref="AttributeTest" /> fields that the Neo inspector filled in Edit Mode, runs the
    ///     equivalent C# scene queries on request and pushes the Rigidbodies the inspector found. It builds no UI.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Attribute Demo")]
    public sealed class AttributeDemoController : MonoBehaviour
    {
        public const int FieldCount = 6;
        public const int RigidbodiesField = 0;
        public const int SpheresField = 1;
        public const int CameraField = 2;
        public const int MoneyField = 3;
        public const int ChildrenField = 4;
        public const int ToggleField = 5;

        /// <summary>Text of an attributed field that holds nothing yet.</summary>
        public const string EmptyText = "(empty) · select Attribute to fill";

        private const int ListedNames = 3;
        private const string MatchSuffix = "  <color=#42D9A3>= C#</color>";
        private const string MismatchSuffix = "  <color=#FF5C7A>differs from C#</color>";

        [Header("Scene")]
        [Tooltip("The hand-authored AttributeTest ('Attribute') whose fields the Neo inspector filled.")]
        [SerializeField] private AttributeTest _attributes;

        [Header("Resolved fields")]
        [SerializeField] private TMP_Text _rigidbodiesValue;
        [SerializeField] private TMP_Text _spheresValue;
        [SerializeField] private TMP_Text _cameraValue;
        [SerializeField] private TMP_Text _moneyValue;
        [SerializeField] private TMP_Text _childrenValue;
        [SerializeField] private TMP_Text _toggleValue;

        [Header("Play Mode")]
        [SerializeField] private TMP_Text _checkValue;
        [SerializeField] private TMP_Text _scoreValue;

        [Tooltip("Upward speed (units per second) Kick bodies gives every Rigidbody in rbsFindAllInScene.")]
        [SerializeField] [Min(0f)] private float _kickSpeed = 6.5f;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private readonly bool[] _matches = new bool[FieldCount];
        private bool _hasCheck;
        private Rigidbody[] _startBodies;
        private Vector3[] _startPositions;
        private Quaternion[] _startRotations;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _attributes != null && _log != null && _rigidbodiesValue != null &&
                                    _spheresValue != null && _cameraValue != null && _moneyValue != null &&
                                    _childrenValue != null && _toggleValue != null && _checkValue != null &&
                                    _scoreValue != null;

        /// <summary>The AttributeTest this demo reads.</summary>
        public AttributeTest Attributes => _attributes;

        /// <summary>Fields equal to their C# query after the last <see cref="CompareWithCSharp" /> (0 before it).</summary>
        public int MatchedFields
        {
            get
            {
                if (!_hasCheck)
                {
                    return 0;
                }

                int matched = 0;
                for (int i = 0; i < FieldCount; i++)
                {
                    if (_matches[i])
                    {
                        matched++;
                    }
                }

                return matched;
            }
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            CaptureStartPoses();
            RefreshFields();
            ShowScore();
            Log("6 attributed fields loaded from the scene · no Find calls");
        }

        /// <summary>Re-reads the attributed fields into the value labels.</summary>
        public void RefreshFields()
        {
            string[] lines = DescribeFields(_attributes);
            TMP_Text[] labels = { _rigidbodiesValue, _spheresValue, _cameraValue, _moneyValue, _childrenValue, _toggleValue };
            for (int i = 0; i < FieldCount; i++)
            {
                if (labels[i] != null)
                {
                    labels[i].text = _hasCheck ? lines[i] + (_matches[i] ? MatchSuffix : MismatchSuffix) : lines[i];
                }
            }
        }

        /// <summary>
        ///     Button target: runs the runtime equivalent of every attribute (FindObjectsByType, FindFirstObjectByType,
        ///     GetComponent, GetComponentsInChildren, GetComponent of the interface) and compares the results with the
        ///     values the inspector saved.
        /// </summary>
        public void CompareWithCSharp()
        {
            if (_attributes == null)
            {
                WarnUnbuilt();
                return;
            }

            _matches[RigidbodiesField] = SameSet(_attributes.rbsFindAllInScene,
                FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            _matches[SpheresField] = SameSet(_attributes.ballsFindAllInScene,
                FindObjectsByType<SphereCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            Camera firstCamera = FindFirstObjectByType<Camera>();
            _matches[CameraField] = _attributes.camFindInScene != null && _attributes.camFindInScene == firstCamera;
            _matches[MoneyField] = _attributes.moneyRequireInterface != null &&
                                   _attributes.moneyRequireInterface.GetComponent<IMoneyAdd>() != null;
            _matches[ChildrenField] = SameSet(_attributes.childrensGetComponents,
                GameObjectsOf(_attributes.GetComponentsInChildren<Transform>()));
            _matches[ToggleField] = _attributes.toggleGetComponent != null &&
                                    _attributes.toggleGetComponent == _attributes.GetComponent<ToggleObject>();
            _hasCheck = true;

            int matched = MatchedFields;
            RefreshFields();
            if (_checkValue != null)
            {
                _checkValue.text = matched + " / " + FieldCount + " equal the C# queries";
            }

            Log("C# check: " + matched + "/" + FieldCount + " equal FindObjectsByType / GetComponent(s)");
        }

        /// <summary>Button target: pushes every Rigidbody of rbsFindAllInScene up (the array is a plain reference list).</summary>
        public void KickBodies()
        {
            if (_attributes == null || _attributes.rbsFindAllInScene == null)
            {
                WarnUnbuilt();
                return;
            }

            Rigidbody[] bodies = _attributes.rbsFindAllInScene;
            int kicked = 0;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body.isKinematic)
                {
                    continue;
                }

                float side = i % 2 == 0 ? -1f : 1f;
                body.AddForce(new Vector3(side * _kickSpeed * 0.18f, _kickSpeed, 0f), ForceMode.VelocityChange);
                body.AddTorque(new Vector3(0f, 0f, -side * 4f), ForceMode.VelocityChange);
                kicked++;
            }

            Log("Kicked " + kicked + " bodies from rbsFindAllInScene");
        }

        /// <summary>Button target: puts the kicked bodies back where they stood when Play Mode started.</summary>
        public void ResetBodies()
        {
            if (_startBodies == null)
            {
                return;
            }

            for (int i = 0; i < _startBodies.Length; i++)
            {
                Rigidbody body = _startBodies[i];
                if (body == null)
                {
                    continue;
                }

                body.transform.SetPositionAndRotation(_startPositions[i], _startRotations[i]);
                body.position = _startPositions[i];
                body.rotation = _startRotations[i];
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }

            Log("Bodies reset to their scene positions");
        }

        /// <summary>Listener after AttributeTest.AddScore / ResetScore: shows and logs the new score.</summary>
        public void RefreshScore()
        {
            ShowScore();
            if (_attributes != null)
            {
                Log("AttributeTest.score = " + _attributes.score);
            }
        }

        /// <summary>One display line per attributed field, in the order of the *Field constants.</summary>
        public static string[] DescribeFields(AttributeTest attributes)
        {
            string[] lines = new string[FieldCount];
            if (attributes == null)
            {
                for (int i = 0; i < FieldCount; i++)
                {
                    lines[i] = "-";
                }

                return lines;
            }

            lines[RigidbodiesField] = DescribeList(attributes.rbsFindAllInScene);
            lines[SpheresField] = DescribeList(attributes.ballsFindAllInScene);
            lines[CameraField] = attributes.camFindInScene != null ? attributes.camFindInScene.name : EmptyText;
            lines[MoneyField] = DescribeMoney(attributes.moneyRequireInterface);
            lines[ChildrenField] = DescribeList(attributes.childrensGetComponents);
            lines[ToggleField] = attributes.toggleGetComponent != null
                ? "ToggleObject on " + attributes.toggleGetComponent.name
                : EmptyText;
            return lines;
        }

        /// <summary>"3 · Cube, Ball (1), Ball" (at most three names, then "+N").</summary>
        public static string DescribeList(IReadOnlyList<Object> items)
        {
            int count = items != null ? items.Count : 0;
            if (count == 0)
            {
                return EmptyText;
            }

            StringBuilder builder = new();
            builder.Append(count).Append(" · ");
            int listed = Mathf.Min(count, ListedNames);
            for (int i = 0; i < listed; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                Object item = items[i];
                builder.Append(item != null ? item.name : "(missing)");
            }

            if (count > listed)
            {
                builder.Append(" +").Append(count - listed);
            }

            return builder.ToString();
        }

        /// <summary>
        ///     True when both lists hold the same non-null objects regardless of order (the inspector sorts by
        ///     instance id, a C# query may not).
        /// </summary>
        public static bool SameSet(IReadOnlyList<Object> stored, IReadOnlyList<Object> found)
        {
            int storedCount = CountAlive(stored);
            if (storedCount != CountAlive(found))
            {
                return false;
            }

            if (storedCount == 0)
            {
                return true;
            }

            for (int i = 0; i < found.Count; i++)
            {
                Object item = found[i];
                if (item != null && !Contains(stored, item))
                {
                    return false;
                }
            }

            return true;
        }

        private static string DescribeMoney(GameObject money)
        {
            if (money == null)
            {
                return EmptyText;
            }

            return money.GetComponent<IMoneyAdd>() != null
                ? money.name + " · implements IMoneyAdd"
                : money.name + " · no IMoneyAdd";
        }

        private static int CountAlive(IReadOnlyList<Object> items)
        {
            int alive = 0;
            if (items == null)
            {
                return 0;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    alive++;
                }
            }

            return alive;
        }

        private static bool Contains(IReadOnlyList<Object> items, Object item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == item)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject[] GameObjectsOf(Transform[] transforms)
        {
            GameObject[] objects = new GameObject[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                objects[i] = transforms[i].gameObject;
            }

            return objects;
        }

        private void CaptureStartPoses()
        {
            if (_attributes == null || _attributes.rbsFindAllInScene == null)
            {
                return;
            }

            _startBodies = (Rigidbody[])_attributes.rbsFindAllInScene.Clone();
            _startPositions = new Vector3[_startBodies.Length];
            _startRotations = new Quaternion[_startBodies.Length];
            for (int i = 0; i < _startBodies.Length; i++)
            {
                if (_startBodies[i] != null)
                {
                    _startBodies[i].transform.GetPositionAndRotation(out _startPositions[i], out _startRotations[i]);
                }
            }
        }

        private void ShowScore()
        {
            if (_scoreValue != null)
            {
                _scoreValue.text = _attributes != null ? _attributes.score.ToString() : "-";
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[AttributeDemoController] The scene UI is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
