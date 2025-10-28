using UnityEngine;

namespace Neo.Tools.View
{
    /// <summary>
    /// Компонент для синхронизации эмиссии меша с источником света.
    /// Копирует интенсивность и цвет от Light компонента в реальном времени.
    /// </summary>
    public class MeshEmission : MonoBehaviour
    {
        [Header("Sync Mode")]
        [Tooltip("Включить синхронизацию с источником света")]
        public bool syncWithLight = true;
        
        [Tooltip("Источник света для синхронизации (может быть на другом объекте)")]
        public Light targetLight;

        [Header("Sync Settings")]
        [Tooltip("Синхронизировать интенсивность")]
        public bool syncIntensity = true;
        
        [Tooltip("Синхронизировать цвет")]
        public bool syncColor = true;

        [Tooltip("Множитель интенсивности (1.0 = точно как у света)")] 
        [Range(0f, 10f)]
        public float intensityMultiplier = 0.1f;

        [Header("Color Enhancement")]
        [Tooltip("Интенсивность, при которой начинается осветление цвета")]
        [Range(0f, 50f)]
        public float whiteThreshold = 10f;
        
        [Tooltip("Диапазон осветления (интенсивность от whiteThreshold до whiteThreshold + whiteRange)")]
        [Range(1f, 50f)]
        public float whiteRange = 10f;

        [Header("Visibility")]
        [Tooltip("Порог, ниже которого эмиссия полностью отключается")]
        [Range(0f, 5f)]
        public float emissionCutoff = 0.3f;

        /// <summary>
        /// Текущая интенсивность эмиссии (только для чтения)
        /// </summary>
        public float CurrentIntensity { get; private set; }

        /// <summary>
        /// Текущий цвет эмиссии (только для чтения)
        /// </summary>
        public Color CurrentColor { get; private set; }

        private MeshRenderer _meshRenderer;
        private Material _material;
        private Color _originalEmissionColor;
        
        // Кэшированные ID для оптимизации (Built-in/URP/HDRP)
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private const string EmissionKW = "_EMISSION";

        void Awake()
        { 
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                Debug.LogError("MeshEmission: No MeshRenderer found on this GameObject!", this);
                enabled = false;
                return;
            }

            _material = _meshRenderer.material;
            _material.EnableKeyword(EmissionKW);
            _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _originalEmissionColor = _material.GetColor(EmissionColorID);
        }

        void Start()
        { 
            CurrentIntensity = 1f;
            CurrentColor = _originalEmissionColor;

            if (syncWithLight && targetLight == null)
            {
                FindAndAttachLight();
            }
        }

        void Update()
        {
            if (_material == null || !syncWithLight || targetLight == null) return;

            float lightIntensity = targetLight.intensity;
            Color lightColor = targetLight.color;
            
            float targetIntensity = syncIntensity ? (lightIntensity * intensityMultiplier) : 1f;
            Color finalEmissionColor = syncColor ? lightColor : _originalEmissionColor;

            // При высокой интенсивности увеличиваем для осветления
            float finalIntensity = targetIntensity;
            if (targetIntensity > whiteThreshold)
            {
                float whiteFactor = Mathf.Clamp01((targetIntensity - whiteThreshold) / whiteRange);
                finalIntensity = targetIntensity * (1f + whiteFactor);
            }

            ApplyEmission(finalEmissionColor, finalIntensity);

            CurrentIntensity = finalIntensity;
            CurrentColor = finalEmissionColor;
        }

        /// <summary>
        /// Универсальная запись эмиссии с автоотключением при малых значениях
        /// </summary>
        private void ApplyEmission(Color color, float intensity)
        {
            if (_material == null) return;

            // МАЛЕНЬКОЕ ЗНАЧЕНИЕ — ПОЛНОЕ ОТКЛЮЧЕНИЕ
            if (intensity <= emissionCutoff)
            {
                if (_material.HasProperty(EmissionColorID)) _material.SetColor(EmissionColorID, Color.black);
                if (_material.HasProperty(EmissiveColorID)) _material.SetColor(EmissiveColorID, Color.black);

                _material.DisableKeyword(EmissionKW);
                _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                return;
            }

            // НОРМАЛЬНАЯ РАБОТА — ВКЛЮЧАЕМ ЭМИССИЮ
            _material.EnableKeyword(EmissionKW);

            if (_material.HasProperty(EmissionColorID))
                _material.SetColor(EmissionColorID, color * intensity);

            if (_material.HasProperty(EmissiveColorID))
                _material.SetColor(EmissiveColorID, color * intensity);

            _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        /// <summary>
        /// Сбросить к исходным значениям эмиссии
        /// </summary>
        public void ResetToOriginal()
        {
            if (_material != null)
            {
                ApplyEmission(_originalEmissionColor, 1f);
                CurrentIntensity = 1f;
                CurrentColor = _originalEmissionColor;
            }
        }

        /// <summary>
        /// Найти и привязать источник света на том же объекте или дочерних
        /// </summary>
        public void FindAndAttachLight()
        {
            targetLight = GetComponent<Light>();
            
            if (targetLight == null)
            {
                targetLight = GetComponentInChildren<Light>();
            }

            if (targetLight != null)
            {
                Debug.Log($"[{gameObject.name}] Found Light: {targetLight.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] No Light found on self or children!");
            }
        }

        private void OnDisable()
        {
            if (_material != null)
            {
                ApplyEmission(_originalEmissionColor, 1f);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                DestroyImmediate(_material);
                _material = null;
            }
        }

        private void OnValidate()
        {
            if (intensityMultiplier < 0f) intensityMultiplier = 0f;
            if (whiteThreshold < 0f) whiteThreshold = 0f;
            if (whiteRange < 1f) whiteRange = 1f;
            if (emissionCutoff < 0f) emissionCutoff = 0f;
        }
    }
}
