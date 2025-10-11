using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using Neo;

namespace Neo
{
    namespace Demo
    {
        public class AttributeTest : MonoBehaviour
        {
            [GUIColor(1,1,0)] [FindAllInScene]
            public Rigidbody[] rbsFindAllInScene;

            [GUIColor(1,0,1)] [FindAllInScene]
            public SphereCollider[] ballsFindAllInScene;

            [FindInScene] public Camera camFindInScene;

            [RequireInterface(typeof(IMoneyAdd))] public GameObject moneyRequireInterface;

            [GetComponents(true)] public GameObject[] childrensGetComponents;

            [GetComponent] public ToggleObject toggleGetComponent;

            [Button]
            public void PrintHello()
            {
                print("Hello World!");
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