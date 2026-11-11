using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Drives Animator parameters from <see cref="PlayerController2DPhysics" /> and Rigidbody2D velocity.
    /// </summary>
    [NeoDoc("Tools/Move/PlayerController2DAnimatorDriver.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/PlayerController2DAnimatorDriver")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PlayerController2DAnimatorDriver))]
    public class PlayerController2DAnimatorDriver : MonoBehaviour
    {
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

        private readonly AnimatorParameterCache _parameters = new();
        private int _parametersVersion = -1;
        private AnimatorParameterHandle _isGroundedHandle;
        private AnimatorParameterHandle _isMovingHandle;
        private AnimatorParameterHandle _isRunningHandle;
        private AnimatorParameterHandle _speedHandle;
        private AnimatorParameterHandle _locomotionStateHandle;
        private AnimatorParameterHandle _jumpTriggerHandle;
        private AnimatorParameterHandle _blendXHandle;
        private AnimatorParameterHandle _blendYHandle;
        private bool _wasGrounded;

        /// <summary>
        ///     Gets whether all required references are assigned.
        /// </summary>
        public bool IsReady => _animator != null && _controller != null && _rigidbody != null;

        private void Awake()
        {
            TryAutoAssignReferences();
        }

        private void OnValidate()
        {
            _parameters.Invalidate();
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

            bool isGrounded = _controller.IsGrounded;
            if (!RefreshParameters())
            {
                // WHY: keep the jump edge detector current while the Animator cannot take writes, so an Animator
                // that initializes mid-air does not fire a Jump trigger from a stale grounded state.
                _wasGrounded = isGrounded;
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;
            bool isMoving = speed > _movingThreshold;
            bool isRunning = _controller.IsRunning && isMoving;

            SetBool(_isGroundedHandle, isGrounded);
            SetBool(_isMovingHandle, isMoving);
            SetBool(_isRunningHandle, isRunning);
            SetFloat(_speedHandle, speed);

            if (_useLocomotionStateInt && _locomotionStateHandle.Exists)
            {
                int state = !isGrounded ? 3 : isRunning ? 2 : isMoving ? 1 : 0;
                _animator.SetInteger(_locomotionStateHandle.Hash, state);
            }

            if (_useJumpTrigger && _jumpTriggerHandle.Exists && _wasGrounded && !isGrounded && velocity.y > 0.05f)
            {
                _animator.SetTrigger(_jumpTriggerHandle.Hash);
            }

            if (_useBlendTree)
            {
                Vector2 blend = CalculateBlend(velocity);
                SetBlendFloat(_blendXHandle, blend.x);
                SetBlendFloat(_blendYHandle, blend.y);
            }

            _wasGrounded = isGrounded;
        }

        /// <summary>
        ///     Re-resolves parameter handles when the Animator or its controller changed. Missing or differently typed
        ///     parameters are skipped instead of producing a Unity warning every frame; each one that is in use is reported
        ///     once per binding through the gated <see cref="NeoDiagnostics" /> warning channel.
        /// </summary>
        private bool RefreshParameters()
        {
            if (!_parameters.Refresh(_animator))
            {
                return false;
            }

            if (_parametersVersion == _parameters.Version)
            {
                return true;
            }

            _parametersVersion = _parameters.Version;
            _isGroundedHandle = _parameters.Resolve(_isGroundedParam, AnimatorControllerParameterType.Bool, this, true);
            _isMovingHandle = _parameters.Resolve(_isMovingParam, AnimatorControllerParameterType.Bool, this, true);
            _isRunningHandle = _parameters.Resolve(_isRunningParam, AnimatorControllerParameterType.Bool, this, true);
            _speedHandle = _parameters.Resolve(_speedParam, AnimatorControllerParameterType.Float, this, true);
            _locomotionStateHandle = _parameters.Resolve(_locomotionStateParam, AnimatorControllerParameterType.Int,
                this, _useLocomotionStateInt);
            _jumpTriggerHandle = _parameters.Resolve(_jumpTriggerParam, AnimatorControllerParameterType.Trigger,
                this, _useJumpTrigger);
            _blendXHandle = _parameters.Resolve(_blendXParam, AnimatorControllerParameterType.Float, this,
                _useBlendTree);
            // WHY: HorizontalOnly writes MoveY as a constant 0, so a controller without it is a valid setup.
            _blendYHandle = _parameters.Resolve(_blendYParam, AnimatorControllerParameterType.Float, this,
                _useBlendTree && _blendMode == BlendMode2D.TwoAxis);
            return true;
        }

        private void SetBool(AnimatorParameterHandle handle, bool value)
        {
            if (handle.Exists)
            {
                _animator.SetBool(handle.Hash, value);
            }
        }

        private void SetFloat(AnimatorParameterHandle handle, float value)
        {
            if (handle.Exists)
            {
                _animator.SetFloat(handle.Hash, value);
            }
        }

        private void SetBlendFloat(AnimatorParameterHandle handle, float value)
        {
            if (!handle.Exists)
            {
                return;
            }

            if (_blendDampTime > 0f)
            {
                _animator.SetFloat(handle.Hash, value, _blendDampTime, Time.deltaTime);
            }
            else
            {
                _animator.SetFloat(handle.Hash, value);
            }
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

        private enum BlendMode2D
        {
            HorizontalOnly,
            TwoAxis
        }
    }
}
