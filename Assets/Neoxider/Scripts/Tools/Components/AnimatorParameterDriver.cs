using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Удобный вызов параметров Animator: триггер, bool, float, int. Используйте из кода или подключайте к UnityEvent.
    /// </summary>
    [NeoDoc("Tools/Components/AnimatorParameterDriver.md")]
    [CreateFromMenu("Neoxider/Tools/Components/AnimatorParameterDriver")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(AnimatorParameterDriver))]
    public sealed class AnimatorParameterDriver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Animator. Если не задан — ищется на этом объекте.")]
        private Animator animator;

        [Header("Имя параметра в переменной (для методов без имени)")]
        [SerializeField]
        [Tooltip("Имя триггера для SetTrigger() без аргумента.")]
        private string triggerParameterName;

        [SerializeField]
        [Tooltip("Имя bool-параметра для SetBool(bool).")]
        private string boolParameterName;

        [SerializeField]
        [Tooltip("Имя float-параметра для SetFloat(float).")]
        private string floatParameterName;

        [SerializeField]
        [Tooltip("Имя int-параметра для SetInt(int).")]
        private string intParameterName;

        private Animator Anim => animator != null ? animator : (animator = GetComponent<Animator>());

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>Вызвать триггер по имени из поля Trigger Parameter Name.</summary>
        public void SetTrigger()
        {
            SetTrigger(triggerParameterName);
        }

        /// <summary>Вызвать триггер по имени.</summary>
        public void SetTrigger(string triggerName)
        {
            if (Anim == null || string.IsNullOrEmpty(triggerName)) return;
            Anim.SetTrigger(triggerName);
        }

        /// <summary>Установить bool-параметр по имени из поля Bool Parameter Name.</summary>
        public void SetBool(bool value)
        {
            SetBool(boolParameterName, value);
        }

        /// <summary>Установить bool-параметр.</summary>
        public void SetBool(string parameterName, bool value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName)) return;
            Anim.SetBool(parameterName, value);
        }

        /// <summary>Установить bool = true (удобно для UnityEvent с одним аргументом — имя параметра).</summary>
        public void SetBoolTrue(string parameterName)
        {
            SetBool(parameterName, true);
        }

        /// <summary>Установить bool = false.</summary>
        public void SetBoolFalse(string parameterName)
        {
            SetBool(parameterName, false);
        }

        /// <summary>Установить float-параметр по имени из поля Float Parameter Name.</summary>
        public void SetFloat(float value)
        {
            SetFloat(floatParameterName, value);
        }

        /// <summary>Установить float-параметр.</summary>
        public void SetFloat(string parameterName, float value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName)) return;
            Anim.SetFloat(parameterName, value);
        }

        /// <summary>Установить int-параметр по имени из поля Int Parameter Name.</summary>
        public void SetInt(int value)
        {
            SetInt(intParameterName, value);
        }

        /// <summary>Установить int-параметр.</summary>
        public void SetInt(string parameterName, int value)
        {
            if (Anim == null || string.IsNullOrEmpty(parameterName)) return;
            Anim.SetInteger(parameterName, value);
        }
    }
}
