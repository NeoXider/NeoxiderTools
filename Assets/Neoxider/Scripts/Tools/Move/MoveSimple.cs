using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class MoveSimple : MonoBehaviour
    {
        public bool isMove = true;
        public MoveController moveController = new();

        [Space]
        public bool inputMouse = false;
        public bool inputKeyboard = false;
        public bool isNormalized = true;
        public bool isUpdateDirection = false;
        public bool useForceMode = false;
        public Vector3 direction = Vector3.zero;
        public Vector3 velocity;

        [SerializeField] private Transform _target;
        [SerializeField] private Rigidbody _rigidbody3D;
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Space]
        [Header("Keyboard Controls")]
        [SerializeField] private KeyCode moveUpKey = KeyCode.W;
        [SerializeField] private KeyCode moveDownKey = KeyCode.S;
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;

        [Space]
        [Header("Events")]
        public UnityEvent OnMove;
        public UnityEvent OnStop;

        public bool _isMoveble;

        public void Reset()
        {
            StopMove();
        }

        void Update()
        {
            if (!isMove) return;

            velocity = isUpdateDirection ? Vector3.zero : direction;

            if (inputMouse)
            {
                if (Input.GetMouseButton(0))
                {
                    Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    velocity = (mousePosition - transform.position).normalized;
                }
            }

            if (inputKeyboard)
            {
                if (Input.GetKey(moveUpKey)) velocity += Vector3.up;
                if (Input.GetKey(moveDownKey)) velocity += Vector3.down;
                if (Input.GetKey(moveLeftKey)) velocity += Vector3.left;
                if (Input.GetKey(moveRightKey)) velocity += Vector3.right;
            }

            if (isNormalized && velocity != Vector3.zero)
                velocity = velocity.normalized;

            if (_target != null)
                velocity = (_target.position - transform.position).normalized;

            direction = velocity;
            Move(direction);

            if (direction != Vector3.zero && !_isMoveble)
            {
                _isMoveble = true;
                OnMove?.Invoke();
            }
            else if (direction == Vector3.zero && isMove)
            {
                _isMoveble = false;
                OnStop?.Invoke();
            }
        }

        public void MoveWithForce(Vector3 velocity)
        {
            if (_rigidbody3D != null)
            {
                _rigidbody3D.AddForce(CalculateForceVector(_rigidbody3D.mass, velocity), ForceMode.VelocityChange);
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.AddForce(CalculateForceVector(_rigidbody2D.mass, velocity), ForceMode2D.Force);
            }
        }

        public void MoveWithVelocity(Vector3 velocity)
        {
            if (_rigidbody3D != null)
            {
                _rigidbody3D.velocity = velocity;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.velocity = velocity;
            }
        }

        private Vector3 CalculateForceVector(float mass, Vector3 velocity)
        {
            Vector3 acceleration = velocity / Time.deltaTime;
            return mass * acceleration;
        }

        public void Move(Vector3 direction)
        {
            Vector3 velocity = moveController.GetVelocity(transform.position, direction, _target);

            if (_rigidbody3D != null || _rigidbody2D != null)
            {
                if (useForceMode)
                    MoveWithForce(velocity);
                else
                    MoveWithVelocity(velocity);
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }

        public void MoveRight() => direction = Vector2.right;
        public void MoveLeft() => direction = Vector2.left;
        public void MoveUp() => direction = Vector2.up;
        public void MoveDown() => direction = Vector2.down;

        public void StopMove()
        {
            direction = Vector2.zero;
            isMove = false;
            OnStop?.Invoke();
        }

        public void StartMove()
        {
            isMove = true;
            OnMove?.Invoke();
        }

        public void SetTarget(Transform target) => _target = target;
    }
}