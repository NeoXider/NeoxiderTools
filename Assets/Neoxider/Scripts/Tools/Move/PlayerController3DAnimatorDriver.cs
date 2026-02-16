using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Drives Animator parameters from <see cref="PlayerController3DPhysics"/> and Rigidbody velocity.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(PlayerController3DAnimatorDriver))]
    public class PlayerController3DAnimatorDriver : MonoBehaviour
    {
        private enum VelocitySpace
        {
            World,
            Local,
            CameraRelative
        }

        [Header("References")] [SerializeField]
        private Animator _animator;

        [SerializeField] private PlayerController3DPhysics _controller;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _cameraTransform;

        [Header("General")] [SerializeField] private bool _updateInLateUpdate = true;
        [SerializeField] private float _movingThreshold = 0.1f;
        [SerializeField] private float _blendMaxSpeed = 7.5f;
        [SerializeField] private VelocitySpace _velocitySpace = VelocitySpace.Local;

        [Header("Simple State Params")] [SerializeField]
        private string _isGroundedParam = "IsGrounded";

        [SerializeField] private string _isMovingParam = "IsMoving";
        [SerializeField] private string _isRunningParam = "IsRunning";
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _locomotionStateParam = "LocomotionState";
        [SerializeField] private string _jumpTriggerParam = "Jump";
        [SerializeField] private bool _useLocomotionStateInt = true;
        [SerializeField] private bool _useJumpTrigger = true;

        [Header("BlendTree Params")] [SerializeField]
        private bool _useDirectionalBlendTree = true;

        [SerializeField] private string _blendXParam = "MoveX";
        [SerializeField] private string _blendYParam = "MoveY";
        [SerializeField] private float _blendDampTime = 0.08f;

        private bool _wasGrounded;

        /// <summary>
        ///     Gets whether all required references are assigned.
        /// </summary>
        public bool IsReady => _animator != null && _controller != null && _rigidbody != null;

        private void Awake()
        {
            TryAutoAssignReferences();
        }

        private void Update()
        {
            if (!_updateInLateUpdate)
            {
                ApplyAnimation();
            }
        }

        private void LateUpdate()
        {
            if (_updateInLateUpdate)
            {
                ApplyAnimation();
            }
        }

        /// <summary>
        ///     Assigns animator reference at runtime.
        /// </summary>
        /// <param name="animator">Animator component to use.</param>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        ///     Assigns movement controller reference at runtime.
        /// </summary>
        /// <param name="controller">Controller component to use.</param>
        public void SetController(PlayerController3DPhysics controller)
        {
            _controller = controller;
        }

        /// <summary>
        ///     Assigns rigidbody reference at runtime.
        /// </summary>
        /// <param name="body">Rigidbody component to use.</param>
        public void SetRigidbody(Rigidbody body)
        {
            _rigidbody = body;
        }

        private void TryAutoAssignReferences()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_controller == null)
            {
                _controller = GetComponent<PlayerController3DPhysics>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void ApplyAnimation()
        {
            if (!IsReady)
            {
                return;
            }

            Vector3 planarVelocity = _rigidbody.velocity;
            planarVelocity.y = 0f;
            float speed = planarVelocity.magnitude;
            bool isMoving = speed > _movingThreshold;
            bool isGrounded = _controller.IsGrounded;
            bool isRunning = _controller.IsRunning && isMoving;

            SetBoolSafe(_isGroundedParam, isGrounded);
            SetBoolSafe(_isMovingParam, isMoving);
            SetBoolSafe(_isRunningParam, isRunning);
            SetFloatSafe(_speedParam, speed);

            if (_useLocomotionStateInt)
            {
                int state = !isGrounded ? 3 : isRunning ? 2 : isMoving ? 1 : 0;
                SetIntSafe(_locomotionStateParam, state);
            }

            if (_useJumpTrigger && _wasGrounded && !isGrounded && _rigidbody.velocity.y > 0.05f)
            {
                SetTriggerSafe(_jumpTriggerParam);
            }

            if (_useDirectionalBlendTree)
            {
                Vector2 blend = CalculateBlend(planarVelocity);
                if (_blendDampTime > 0f)
                {
                    _animator.SetFloat(_blendXParam, blend.x, _blendDampTime, Time.deltaTime);
                    _animator.SetFloat(_blendYParam, blend.y, _blendDampTime, Time.deltaTime);
                }
                else
                {
                    _animator.SetFloat(_blendXParam, blend.x);
                    _animator.SetFloat(_blendYParam, blend.y);
                }
            }

            _wasGrounded = isGrounded;
        }

        private Vector2 CalculateBlend(Vector3 worldPlanarVelocity)
        {
            if (worldPlanarVelocity.sqrMagnitude <= 0.0001f || _blendMaxSpeed <= 0.01f)
            {
                return Vector2.zero;
            }

            Vector3 basisForward;
            Vector3 basisRight;
            switch (_velocitySpace)
            {
                case VelocitySpace.World:
                    basisForward = Vector3.forward;
                    basisRight = Vector3.right;
                    break;
                case VelocitySpace.CameraRelative:
                    if (_cameraTransform == null)
                    {
                        basisForward = transform.forward;
                        basisRight = transform.right;
                    }
                    else
                    {
                        basisForward = _cameraTransform.forward;
                        basisRight = _cameraTransform.right;
                    }

                    basisForward.y = 0f;
                    basisRight.y = 0f;
                    basisForward.Normalize();
                    basisRight.Normalize();
                    break;
                default:
                    basisForward = transform.forward;
                    basisRight = transform.right;
                    basisForward.y = 0f;
                    basisRight.y = 0f;
                    basisForward.Normalize();
                    basisRight.Normalize();
                    break;
            }

            float x = Vector3.Dot(worldPlanarVelocity, basisRight) / _blendMaxSpeed;
            float y = Vector3.Dot(worldPlanarVelocity, basisForward) / _blendMaxSpeed;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void SetBoolSafe(string param, bool value)
        {
            if (!string.IsNullOrWhiteSpace(param))
            {
                _animator.SetBool(param, value);
            }
        }

        private void SetFloatSafe(string param, float value)
        {
            if (!string.IsNullOrWhiteSpace(param))
            {
                _animator.SetFloat(param, value);
            }
        }

        private void SetIntSafe(string param, int value)
        {
            if (!string.IsNullOrWhiteSpace(param))
            {
                _animator.SetInteger(param, value);
            }
        }

        private void SetTriggerSafe(string param)
        {
            if (!string.IsNullOrWhiteSpace(param))
            {
                _animator.SetTrigger(param);
            }
        }
    }
}