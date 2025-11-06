using UnityEngine;

namespace Neo.Tools{
/// <summary>
///     Базовый контракт для любых систем перемещения.
/// </summary>
public interface IMover
{
    /// <summary>Движется ли объект прямо сейчас.</summary>
    bool IsMoving { get; }

    /// <summary>Прямое задание мирового смещения (м/кадр).</summary>
    void MoveDelta(Vector2 delta);

    /// <summary>Движение к точке назначения.</summary>
    void MoveToPoint(Vector2 worldTarget);
}
}