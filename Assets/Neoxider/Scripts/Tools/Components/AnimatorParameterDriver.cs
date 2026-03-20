using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Convenient driver for Animator parameters: trigger, bool, float, int. Use from code or wire to UnityEvent.</summary>
    [NeoDoc("Tools/Components/AnimatorParameterDriver.md")]
    [CreateFromMenu("Neoxider/Tools/Components/AnimatorParameterDriver")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(AnimatorParameterDriver))]
    public sealed class AnimatorParameterDriver : MonoBehaviour
    {
        [SerializeField] [Tooltip("Target Animator. If not set, GetComponent on this object.")]
        private Animator animator;

        [Header("Parameter names (for methods without name argument)")]
        [SerializeField]
        [Tooltip("Trigger parameter name for SetTrigger() when called with no argument.")]
        private string triggerParameterName;

        [SerializeField] [Tooltip("Bool parameter name for SetBool(bool).")]
        private string boolParameterName;

        [SerializeField] [Tooltip("Float parameter name for SetFloat(float).")]
        private string floatParameterName;

        [SerializeField] [Tooltip("Int parameter name for SetInt(int).")]
        private string intParameterName;

        private Animator Anim => animator != null ? animator : animator = GetComponent<Animator>();

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>Fires the trigger from the Trigger Parameter Name field.</summary>
        public void SetTrigger()
        {
            SetTrigger(triggerParameterName);
        }

        /// <summary>Fires the trigger by name.</summary>
        /// <param name="triggerName">Parameter name in the Animator.</param>
        public void SetTrigger(string triggerName)
        {
            if (Anim == null || string.IsNullOrEmpty(triggerName))
            {
                return;
            }

            Anim.SetTrigger(triggerName);
        }

        /// <summary>Sets bool parameter from the Bool Parameter Name field.</summary>
        public void SetBool(bool value)
        {
            SetBool(boolParameterName, value);
        }

        /// <summary>Sets a bool parameter by name.</summary>
        public void SetBool(string parameterName, bool value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            Anim.SetBool(parameterName, value);
        }

        /// <summary>Sets bool parameter to true (convenient for UnityEvent with one argument — parameter name).</summary>
        public void SetBoolTrue(string parameterName)
        {
            SetBool(parameterName, true);
        }

        /// <summary>Sets bool parameter to false.</summary>
        public void SetBoolFalse(string parameterName)
        {
            SetBool(parameterName, false);
        }

        /// <summary>Sets float parameter from the Float Parameter Name field.</summary>
        public void SetFloat(float value)
        {
            SetFloat(floatParameterName, value);
        }

        /// <summary>Sets a float parameter by name.</summary>
        public void SetFloat(string parameterName, float value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            Anim.SetFloat(parameterName, value);
        }

        /// <summary>Sets int parameter from the Int Parameter Name field.</summary>
        public void SetInt(int value)
        {
            SetInt(intParameterName, value);
        }

        /// <summary>Sets an int parameter by name.</summary>
        public void SetInt(string parameterName, int value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            Anim.SetInteger(parameterName, value);
        }
    }
}
