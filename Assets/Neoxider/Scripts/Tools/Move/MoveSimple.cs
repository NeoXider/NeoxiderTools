using UnityEngine;


namespace Neoxider
{
    namespace Tools
    {
        namespace Move
        {
            public class MoveSimple : MonoBehaviour
            {
                public MoveControll move = new();

                public bool inputMouse = false;

                public Vector2 direction = Vector2.zero;

                public void Reset()
                {
                    direction = Vector2.zero;
                }

                void Update()
                {
                    if (inputMouse)
                    {
                        if (!Input.GetMouseButton(0)) return;

                        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                        transform.position = move.MoveUpdate(transform.position, mousePosition);
                    }
                    else
                    {
                        transform.position = move.MoveUpdate(transform.position, transform.position + (Vector3)direction);
                    }
                }

                public void MoveRight()
                {
                    direction = Vector2.right;
                }

                public void MoveLeft()
                {
                    direction = Vector2.left;
                }
            }
        }
    }
}