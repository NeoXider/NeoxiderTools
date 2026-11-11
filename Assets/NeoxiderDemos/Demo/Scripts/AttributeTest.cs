using Neo.Shop;
using Neo.Tools;
using UnityEngine;

namespace Neo
{
    namespace Demo
    {
        /// <summary>
        ///     Showcase component of the inspector-attribute demo (Scenes/Tools/AttributeExample.unity). Every field
        ///     is filled by the Neo inspector when the object is selected in Edit Mode and saved with the scene;
        ///     the [Button] methods become inspector buttons with parameter fields.
        /// </summary>
        public class AttributeTest : MonoBehaviour
        {
            [GUIColor(1, 1, 0)] [FindAllInScene] public Rigidbody[] rbsFindAllInScene;

            [GUIColor(1, 0, 1)] [FindAllInScene] public SphereCollider[] ballsFindAllInScene;

            [FindInScene] public Camera camFindInScene;

            [RequireInterface(typeof(IMoneyAdd))] public GameObject moneyRequireInterface;

            [GetComponents(true)] public GameObject[] childrensGetComponents;

            [GetComponent] public ToggleObject toggleGetComponent;

            [Tooltip("Changed by the Add Score button. In Edit Mode Ctrl+Z reverts it: [Button] calls are recorded " +
                     "with Undo and run on every selected object.")]
            public int score;

            [Button]
            public void PrintHello()
            {
                print("Hello World!");
            }

            /// <summary>Adds <paramref name="amount" /> to <see cref="score" /> (inspector button and UI target).</summary>
            [Button("Add Score")]
            public void AddScore(int amount = 10)
            {
                score += amount;
            }

            /// <summary>Sets <see cref="score" /> back to zero (inspector button and UI target).</summary>
            [Button("Reset Score")]
            public void ResetScore()
            {
                score = 0;
            }

            [Button]
            private void GetMoney(Money money)
            {
                print(money.money);
            }

            [Button]
            private void Say(string message = "message")
            {
                print(message);
            }

            [Button]
            private void Say(GameObject obj)
            {
                print(obj.name);
            }

            [Button]
            private void Say(float value, int precision)
            {
                print(value + ", " + precision);
            }

            [Button]
            private void Say(Vector3 pos)
            {
                print(pos);
            }

            [Button]
            private void Say(bool value)
            {
                print(value);
            }
        }
    }
}
