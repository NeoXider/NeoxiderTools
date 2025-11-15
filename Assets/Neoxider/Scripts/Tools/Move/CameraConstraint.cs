using UnityEngine;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/" + "Tools/" + nameof(CameraConstraint))]
    public class CameraConstraint : MonoBehaviour
    {
        public SpriteRenderer mapSprite; // Спрайт карты
        public Camera cam; // Ссылка на камеру 

        [Header("Дополнительные настройки")] [Tooltip("Отступ от края карты")]
        public float edgePadding; // Отступ от краев спрайта

        public bool constraintX = true; // Ограничить горизонтальное перемещение
        public bool constraintY = true; // Ограничить вертикальное перемещение
        public bool showDebugGizmos = true; // Показывать визуальные ограничения в редакторе
        private float camHeight, camWidth;

        private float minX, maxX, minY, maxY;

        private void Start()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            if (mapSprite == null)
            {
                Debug.LogError("Спрайт карты не назначен!");
                enabled = false;
                return;
            }

            CalculateBounds();
        }

        private void LateUpdate()
        {
            ConstrainCamera();
        }

        // Визуализация границ в редакторе Unity
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || mapSprite == null || cam == null)
            {
                return;
            }

            Gizmos.color = Color.green;

            if (Application.isPlaying)
            {
                // Рисуем прямоугольник ограничений
                Vector3 topLeft = new(minX, maxY, 0);
                Vector3 topRight = new(maxX, maxY, 0);
                Vector3 bottomLeft = new(minX, minY, 0);
                Vector3 bottomRight = new(maxX, minY, 0);

                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);
            }
            else
            {
                // Расчет приблизительных границ для отображения в редакторе
                Camera editorCam = cam;
                float height = editorCam.orthographicSize;
                float width = height * editorCam.aspect;

                Bounds bounds = mapSprite.bounds;
                float minX = bounds.min.x + width + edgePadding;
                float maxX = bounds.max.x - width - edgePadding;
                float minY = bounds.min.y + height + edgePadding;
                float maxY = bounds.max.y - height - edgePadding;

                if (minX > maxX)
                {
                    float centerX = bounds.center.x;
                    minX = maxX = centerX;
                }

                if (minY > maxY)
                {
                    float centerY = bounds.center.y;
                    minY = maxY = centerY;
                }

                Vector3 topLeft = new(minX, maxY, 0);
                Vector3 topRight = new(maxX, maxY, 0);
                Vector3 bottomLeft = new(minX, minY, 0);
                Vector3 bottomRight = new(maxX, minY, 0);

                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);
            }
        }

        private void CalculateBounds()
        {
            // Расчет высоты и ширины области видимости камеры
            camHeight = cam.orthographicSize;
            camWidth = camHeight * cam.aspect;

            // Получаем границы спрайта в мировых координатах
            Bounds bounds = mapSprite.bounds;

            // Вычисляем минимальные и максимальные координаты с учетом размера камеры и отступа
            minX = bounds.min.x + camWidth + edgePadding;
            maxX = bounds.max.x - camWidth - edgePadding;
            minY = bounds.min.y + camHeight + edgePadding;
            maxY = bounds.max.y - camHeight - edgePadding;

            // Если карта слишком маленькая для заданных ограничений
            if (minX > maxX)
            {
                // Центрируем камеру по X
                float centerX = bounds.center.x;
                minX = maxX = centerX;
            }

            if (minY > maxY)
            {
                // Центрируем камеру по Y
                float centerY = bounds.center.y;
                minY = maxY = centerY;
            }
        }

        private void ConstrainCamera()
        {
            Vector3 position = transform.position;

            if (constraintX)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (constraintY)
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            transform.position = position;
        }

        // Если изменился размер карты или камеры, пересчитываем границы
        public void UpdateBounds()
        {
            CalculateBounds();
        }
    }
}