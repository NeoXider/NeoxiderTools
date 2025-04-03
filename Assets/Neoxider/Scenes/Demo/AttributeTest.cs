using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        }
    }
}