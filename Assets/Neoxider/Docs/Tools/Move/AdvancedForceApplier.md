# AdvancedForceApplier

Универсальный компонент для приложения физических сил к 2D и 3D объектам с расширенными возможностями настройки направления, ограничений скорости и случайных параметров.

- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/AdvancedForceApplier.cs`

## Введение
`AdvancedForceApplier` решает задачу применения физических сил (импульсы, ускорения, отталкивания) с гибкой настройкой через инспектор. Поддерживает автоматическое определение типа тела (2D/3D), различные режимы направления, ограничения скорости и случайные параметры силы.

## Ключевые особенности
- Автоматическое определение типа Rigidbody (2D/3D) или ручной выбор
- Множественные режимы направления: скорость, трансформ, кастомный вектор, к цели
- Случайная сила с настраиваемым диапазоном
- Ограничение максимальной скорости после применения силы
- Инверсия направления и локальные/мировые координаты
- Удобный Odin-инспектор с группировкой и условными полями

## Настройки инспектора

### Компоненты
- **Rigidbody3D**: Ссылка на 3D физическое тело
- **Rigidbody2D**: Ссылка на 2D физическое тело

### Общие
- **Body Type**: `Auto` (автоопределение), `Rigidbody3D`, `Rigidbody2D`

### Сила
- **Base Force (N)**: Величина силы по умолчанию
- **Randomize Force**: Включить случайный выбор силы из диапазона
- **Force Range**: Минимальная и максимальная сила (при включенной случайности)
- **Play On Awake**: Применить силу автоматически при старте
- **Force Mode (3D/2D)**: Режим применения силы (Impulse, Force, Acceleration, VelocityChange)

### Ограничения
- **Clamp Max Speed**: Включить лимит максимальной скорости
- **Max Speed**: Максимальная скорость после применения силы

### Направление
- **Direction Source**: `Velocity` (скорость), `TransformForward` (направление трансформа), `CustomVector` (пользовательский), `ToTarget` (к цели)
- **Use Local Forward**: Использовать локальные координаты трансформа
- **Invert Direction**: Обратить направление силы
- **Custom Vector**: Направление при режиме `CustomVector`
- **Target (Transform)**: Целевой объект при режиме `ToTarget`

### Отладка
- **Active Body Type**: Показывает текущий активный тип Rigidbody
- Предупреждение при отсутствии подходящего Rigidbody

### Управление
- **Apply Now**: Кнопка для тестирования применения силы (только в Play Mode)

## Публичные методы
- `void ApplyForce(float force = 0f, Vector3? direction = null)`
  - Применяет силу к объекту. Если `force <= 0`, используется базовая сила (или случайная, если включена). Если `direction == null`, вычисляется по настройкам направления.

## Unity Events
- `OnApplyForce`: Вызывается при успешном применении силы (без параметров).

## Примеры использования

### 1) Отталкивание врага при атаке
```csharp
public class AttackSystem : MonoBehaviour
{
    [SerializeField] private AdvancedForceApplier forceApplier;
    [SerializeField] private float knockbackForce = 15f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Отталкиваем врага в направлении атаки
            Vector3 attackDirection = (other.transform.position - transform.position).normalized;
            forceApplier.ApplyForce(knockbackForce, attackDirection);
        }
    }
}
```

### 2) Случайный взрыв с ограничением скорости
```csharp
public class Explosion : MonoBehaviour
{
    [SerializeField] private AdvancedForceApplier forceApplier;
    
    private void Start()
    {
        // Настройки в инспекторе:
        // - randomizeForce = true
        // - forceRange = (10f, 25f)
        // - clampMaxSpeed = true
        // - maxSpeed = 15f
        // - directionMode = TransformForward
        
        forceApplier.ApplyForce(); // Использует случайную силу из диапазона
    }
}
```

### 3) Притяжение к цели
```csharp
public class Magnet : MonoBehaviour
{
    [SerializeField] private AdvancedForceApplier forceApplier;
    [SerializeField] private Transform target;
    
    private void Start()
    {
        // Настройки в инспекторе:
        // - directionMode = ToTarget
        // - target = цель
        // - invertDirection = true (притяжение вместо отталкивания)
        
        forceApplier.ApplyForce(8f);
    }
}
```
