using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Drives Animator parameters from <see cref="PlayerController3DPhysics" /> and Rigidbody velocity.
    /// </summary>
    /// <remarks>
    ///     <b>Legacy.</b> Pairs with the legacy <see cref="PlayerController3DPhysics" />. The CMF-based controller uses
    ///     CMF's own <c>AnimationControl</c> instead — see <c>Docs/Tools/Move/CharacterController/README.md</c>.
    /// </remarks>
    [LegacyComponent("CMF.AnimationControl (Tools/Move/CharacterController)", false)]
    [NeoDoc("Tools/Move/PlayerController3DAnimatorDriver.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/Legacy/PlayerController3DAnimatorDriver")]
    [AddComponentMenu("Neoxider/" + "Tools/" + "Legacy/" + nameof(PlayerController3DAnimatorDriver))]
    public class PlayerController3DAnimatorDriver : MonoBehaviour
    {
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

            bool isGrounded = _controller.IsGrounded;
            if (!RefreshParameters())
            {
                // WHY: keep the jump edge detector current while the Animator cannot take writes, so an Animator
                // that initializes mid-air does not fire a Jump trigger from a stale grounded state.
                _wasGrounded = isGrounded;
                return;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 planarVelocity = velocity;
            planarVelocity.y = 0f;
            float speed = planarVelocity.magnitude;
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

            if (_useDirectionalBlendTree)
            {
                Vector2 blend = CalculateBlend(planarVelocity);
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
                _useDirectionalBlendTree);
            _blendYHandle = _parameters.Resolve(_blendYParam, AnimatorControllerParameterType.Float, this,
                _useDirectionalBlendTree);
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

        private enum VelocitySpace
        {
            World,
            Local,
            CameraRelative
        }
    }
}
