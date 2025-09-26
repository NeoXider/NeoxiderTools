// using UnityEngine;
//
// namespace Neo.Tools
// {
//     public class DirectionAlignedObject : MonoBehaviour
//     {
//         [Header("Настройки цели")]
//         [SerializeField] private MovementController targetMovementController; // Контроллер движения цели
//         [SerializeField] private Transform targetTransform;                   // Трансформ цели (резервный вариант)
//
//         [Header("Настройки позиции")]
//         [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, -5f); // Смещение относительно направления движения
//
//         [Header("Настройки поворота")]
//         [SerializeField] private bool useRotation = true;        // Использовать ли поворот объекта
//         [SerializeField] private bool is2D = false;              // Использовать 2D-поворот (для 2D-игр)
//         [SerializeField] private Vector3 rotationOffset = Vector3.zero; // Дополнительное смещение поворота
//
//         private void Update()
//         {
//             if (targetMovementController == null && targetTransform == null)
//             {
//                 Debug.LogWarning("Не указана цель для DirectionAlignedObject.");
//                 return;
//             }
//
//             // Получаем направление движения цели
//             Vector3 direction = GetTargetDirection();
//
//             if (direction == Vector3.zero)
//             {
//                 return; // Нет движения — обновление не требуется
//             }
//
//             // Вычисляем позицию с учетом смещения
//             Vector3 offsetPosition = targetTransform.position + (direction.normalized * positionOffset.magnitude);
//
//             // Устанавливаем позицию
//             transform.position = offsetPosition;
//
//             // Применяем поворот, если он включен
//             if (useRotation)
//             {
//                 if (is2D)
//                 {
//                     // 2D-поворот: смотрим на цель в плоскости 2D
//                     Vector2 lookDirection = (targetTransform.position - transform.position).normalized;
//                     float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
//                     transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset.z);
//                 }
//                 else
//                 {
//                     // 3D-поворот: смотрим на цель
//                     transform.LookAt(targetTransform);
//                     transform.Rotate(rotationOffset);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Получает направление движения от контроллера движения цели или использует её переднее направление.
//         /// </summary>
//         private Vector3 GetTargetDirection()
//         {
//             if (targetMovementController != null)
//             {
//                 // Используем скорость из контроллера движения
//                 return targetMovementController.CalculateVelocity();
//             }
//             else if (targetTransform != null)
//             {
//                 // Резервный вариант — переднее направление цели
//                 return targetTransform.forward;
//             }
//             return Vector3.zero;
//         }
//
//         /// <summary>
//         /// Устанавливает цель для компонента. Может вызываться из других скриптов или UnityEvents.
//         /// </summary>
//         public void SetTarget(Transform newTarget)
//         {
//             targetTransform = newTarget;
//             targetMovementController = newTarget.GetComponent<MovementController>();
//         }
//     }
// }

