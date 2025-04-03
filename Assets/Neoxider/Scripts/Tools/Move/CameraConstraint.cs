using UnityEngine;

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
            cam = GetComponent<Camera>();

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
            return;

        Gizmos.color = Color.green;

        if (Application.isPlaying)
        {
            // Рисуем прямоугольник ограничений
            var topLeft = new Vector3(minX, maxY, 0);
            var topRight = new Vector3(maxX, maxY, 0);
            var bottomLeft = new Vector3(minX, minY, 0);
            var bottomRight = new Vector3(maxX, minY, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
        else
        {
            // Расчет приблизительных границ для отображения в редакторе
            var editorCam = cam;
            var height = editorCam.orthographicSize;
            var width = height * editorCam.aspect;

            var bounds = mapSprite.bounds;
            var minX = bounds.min.x + width + edgePadding;
            var maxX = bounds.max.x - width - edgePadding;
            var minY = bounds.min.y + height + edgePadding;
            var maxY = bounds.max.y - height - edgePadding;

            if (minX > maxX)
            {
                var centerX = bounds.center.x;
                minX = maxX = centerX;
            }

            if (minY > maxY)
            {
                var centerY = bounds.center.y;
                minY = maxY = centerY;
            }

            var topLeft = new Vector3(minX, maxY, 0);
            var topRight = new Vector3(maxX, maxY, 0);
            var bottomLeft = new Vector3(minX, minY, 0);
            var bottomRight = new Vector3(maxX, minY, 0);

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
        var bounds = mapSprite.bounds;

        // Вычисляем минимальные и максимальные координаты с учетом размера камеры и отступа
        minX = bounds.min.x + camWidth + edgePadding;
        maxX = bounds.max.x - camWidth - edgePadding;
        minY = bounds.min.y + camHeight + edgePadding;
        maxY = bounds.max.y - camHeight - edgePadding;

        // Если карта слишком маленькая для заданных ограничений
        if (minX > maxX)
        {
            // Центрируем камеру по X
            var centerX = bounds.center.x;
            minX = maxX = centerX;
        }

        if (minY > maxY)
        {
            // Центрируем камеру по Y
            var centerY = bounds.center.y;
            minY = maxY = centerY;
        }
    }

    private void ConstrainCamera()
    {
        var position = transform.position;

        if (constraintX)
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (constraintY)
            position.y = Mathf.Clamp(position.y, minY, maxY);

        transform.position = position;
    }

    // Если изменился размер карты или камеры, пересчитываем границы
    public void UpdateBounds()
    {
        CalculateBounds();
    }
}