using UnityEngine;

namespace Neo
{
    [System.Serializable]
    public class CreateSceneHierarchy
    {
        [SerializeField]
        private string prefixSufix = "===";
        [SerializeField]
        private string[] hierarchyStructure = { "Audio", "GameManager", "Game", "Save", "Settings", "Echonomy", "Bonus", "Other", "UI" };

        public void CreateHierarchy()
        {
            foreach (var name in hierarchyStructure)
            {
                string newName = $"{prefixSufix}{name}{prefixSufix}";
                if (GameObject.Find(newName) == null)
                {
                    GameObject newObject = CreateGameObject(newName);
                }
            }
        }

        private static GameObject CreateGameObject(string name)
        {
            GameObject newObject = new GameObject(name);
            newObject.transform.position = Vector3.zero;
            return newObject;
        }
    }
}
