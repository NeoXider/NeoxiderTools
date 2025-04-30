using Neo.Shop;
using Neo.Tools;
using UnityEngine;

namespace Neo
{
    namespace Demo
    {
        public class AttributeTest : MonoBehaviour
        {
            [Color(ColorEnum.SoftOrange)]
            [FindAllInScene] public Rigidbody[] rbsFindAllInScene;
            
            [Color(ColorEnum.SoftPurple)]
            [FindAllInScene] public SphereCollider[] ballsFindAllInScene;
            
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
            void GetMoney(Money money)
            {
                print(money.money);
            }

            [Button]
            void Say(string message = "message")
            {
                print(message);
            }
            
            [Button]
            void Say(GameObject obj)
            {
                print(obj.name);
            }
            
            [Button]
            void Say(float value, int precision)
            {
                print(value + ", " + precision);
            }
            
            [Button]
            void Say(Vector3 pos)
            {
                print(pos);
            }
            
            [Button]
            void Say(bool value)
            {
                print(value);
            }
        }
    }
}