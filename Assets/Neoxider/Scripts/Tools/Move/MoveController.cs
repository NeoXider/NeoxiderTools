using UnityEngine;

namespace Neo.Tools
{
    [System.Serializable]
    public class MoveController
    {
        public float moveSpeed = 5f;
        public bool useLerp = true;

        [Header("Limits")]
        [Space]
        public AxisLimit xLimit = new AxisLimit(true, false, new Vector2(-10f, 10f));
        public AxisLimit yLimit = new AxisLimit(true, false, new Vector2(-10f, 10f));
        public AxisLimit zLimit = new AxisLimit(false, false, new Vector2(-10f, 10f));

        [Space]
        public float dampingFactor = 0;
        public float stopDistance = 0f;

        public Vector3 MoveUpdate(Vector3 startPos, Vector3 direction)
        {
            Vector3 newPosition = GetDirectionLimit(startPos, ref direction);

            if (useLerp)
            {
                return Vector3.Lerp(startPos, newPosition, moveSpeed * Time.deltaTime);
            }
            else
            {
                return Vector3.MoveTowards(startPos, newPosition, moveSpeed * Time.deltaTime);
            }
        }

        public Vector3 GetVelocity(Vector3 startPos, Vector3 direction, Transform target = null)
        {
            if (direction == Vector3.zero) return Vector3.zero;

            if (stopDistance != 0 && target != null)
                if (Vector3.Distance(startPos, target.position) <= stopDistance)
                {
                    return Vector3.zero;
                }

            return direction * moveSpeed;
        }

        public Vector3 GetDirectionLimit(Vector3 startPos, ref Vector3 direction)
        {
            direction = CheckDistance(startPos, direction);
            Vector3 target = startPos + direction * moveSpeed;
            Vector3 newPosition = startPos;

            if (xLimit.move)
            {
                newPosition.x = xLimit.useLimit ? Mathf.Clamp(target.x, xLimit.limit.x, xLimit.limit.y) : target.x;
            }
            if (yLimit.move)
            {
                newPosition.y = yLimit.useLimit ? Mathf.Clamp(target.y, yLimit.limit.x, yLimit.limit.y) : target.y;
            }
            if (zLimit.move)
            {
                newPosition.z = zLimit.useLimit ? Mathf.Clamp(target.z, zLimit.limit.x, zLimit.limit.y) : target.z;
            }

            return newPosition;
        }

        private Vector3 CheckDistance(Vector3 startPos, Vector3 target)
        {
            float distance = Vector3.Distance(startPos, target);

            if (distance <= stopDistance)
            {
                return Vector3.zero;
            }

            return target;
        }

        [System.Serializable]
        public class AxisLimit
        {
            public bool move;
            public bool useLimit;
            public Vector2 limit;

            public AxisLimit(bool move, bool useLimit, Vector2 limit)
            {
                this.move = move;
                this.useLimit = useLimit;
                this.limit = limit;
            }
        }
    }
}