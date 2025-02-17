using UnityEngine;

namespace Neo.Tools
{
    public class SimpleSpawner : MonoBehaviour
    {
        public GameObject prefab;
        public Vector3 ofset = Vector3.zero;
        public Vector3 eulerAngle = Vector3.zero;

        public bool useParent = true;

        public void Spawn()
        {
            print("Spawn");
            Instantiate(prefab, transform.position + ofset, Quaternion.Euler(eulerAngle), useParent ? transform : transform.root);
        }
    }
}
