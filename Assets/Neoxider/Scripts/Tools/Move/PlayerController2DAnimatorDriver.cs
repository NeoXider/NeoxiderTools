using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Drives Animator parameters from <see cref="PlayerController2DPhysics"/> and Rigidbody2D velocity.
    /// </summary>
    [NeoDoc("Tools/Move/PlayerController2DAnimatorDriver.md")]
    [AddComponentMenu("Neo/" + "Tools/" + nameof(PlayerController2DAnimatorDriver))]
    public class PlayerController2DAnimatorDriver : MonoBehaviour
    {
        private enum BlendMode2D
        {
            HorizontalOnly,
            TwoAxis
        }

        [Header("References")] [SerializeField]
        private Animator _animator;

        [SerializeField] private PlayerController2DPhysics _controller;
        [SerializeField] private Rigidbody2D _rigidbody;

        [Header("General")] [SerializeField] private bool _updateInLateUpdate = true;
        [SerializeField] private float _movingThreshold = 0.1f;
        [SerializeField] private float _blendMaxSpeed = 8f;

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
        private bool _useBlendTree = true;

        [SerializeField] private BlendMode2D _blendMode = BlendMode2D.HorizontalOnly;
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
        public void SetController(PlayerController2DPhysics controller)
        {
            _controller = controller;
        }

        /// <summary>
        ///     Assigns rigidbody reference at runtime.
        /// </summary>
        /// <param name="body">Rigidbody2D component to use.</param>
        public void SetRigidbody(Rigidbody2D body)
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
                _controller = GetComponent<PlayerController2DPhysics>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }
        }

        private void ApplyAnimation()
        {
            if (!IsReady)
            {
                return;
            }

            Vector2 velocity = _rigidbody.velocity;
            float speed = velocity.magnitude;
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

            if (_useBlendTree)
            {
                Vector2 blend = CalculateBlend(velocity);
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

        private Vector2 CalculateBlend(Vector2 velocity)
        {
            if (_blendMaxSpeed <= 0.01f)
            {
                return Vector2.zero;
            }

            float x = Mathf.Clamp(velocity.x / _blendMaxSpeed, -1f, 1f);
            if (_blendMode == BlendMode2D.HorizontalOnly)
            {
                return new Vector2(x, 0f);
            }

            float y = Mathf.Clamp(velocity.y / _blendMaxSpeed, -1f, 1f);
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