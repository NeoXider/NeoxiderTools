using UnityEngine;

namespace Neoxider
{
    namespace Tools
    {
        namespace Move
        {
            [System.Serializable]
            public class MoveControll
            {
                public float moveSpeed = 5f;
                public bool useLerp = true;

                [Header("Limits")]
                [Space]
                public bool moveX = true;
                public bool useLimitX = false;
                public Vector2 xLimit = new Vector2(-10f, 10f);

                [Space]
                public bool moveY = true;
                public bool useLimitY = false;
                public Vector2 yLimit = new Vector2(-10f, 10f);

                [Space]
                public bool moveZ = false;
                public bool useLimitZ = false;
                public Vector2 zLimit = new Vector2(-10f, 10f);


                public Vector3 MoveUpdate(Vector3 startPos, Vector3 targetPos)
                {
                    Vector3 newPosition = startPos;

                    if (moveX)
                    {
                        newPosition.x = useLimitX ? Mathf.Clamp(targetPos.x, xLimit.x, xLimit.y) : targetPos.x;
                    }
                    if (moveY)
                    {
                        newPosition.y = useLimitY ? Mathf.Clamp(targetPos.y, yLimit.x, yLimit.y) : targetPos.y;
                    }
                    if (moveZ)
                    {
                        newPosition.z = useLimitZ ? Mathf.Clamp(targetPos.z, zLimit.x, zLimit.y) : targetPos.z;
                    }

                    if (useLerp)
                    {
                        return Vector3.Lerp(startPos, newPosition, moveSpeed * Time.deltaTime);
                    }
                    else
                    {
                        return Vector3.MoveTowards(startPos, newPosition, moveSpeed * Time.deltaTime);
                    }
                }
            }
        }
    }
}
