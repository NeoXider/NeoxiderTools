using System;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Описывает одну ячейку поля. Универсален для 2D/3D, поддерживает пользовательские типы и данные.
    /// </summary>
    [Serializable]
    public class FieldCell
    {
        /// <summary>
        ///     Координаты ячейки в сетке (2D или 3D).
        /// </summary>
        public Vector3Int Position;

        /// <summary>
        ///     Тип ячейки (проходимая, заблокированная, пользовательские типы).
        /// </summary>
        public int Type;

        /// <summary>
        ///     Проходима ли ячейка для поиска пути.
        /// </summary>
        public bool IsWalkable;

        /// <summary>
        ///     Пользовательские данные (можно расширять через наследование или composition).
        /// </summary>
        public object UserData;

        public FieldCell(Vector3Int position, int type = 0, bool isWalkable = true, object userData = null)
        {
            Position = position;
            Type = type;
            IsWalkable = isWalkable;
            UserData = userData;
        }
    }
}