using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    /// Вспомогательный компонент, который хранит ссылку на пул, которому принадлежит этот объект.
    /// </summary>
    [AddComponentMenu("")] // Скрываем из меню компонентов
    public class PooledObjectInfo : MonoBehaviour
    {
        public NeoObjectPool OwnerPool { get; set; }
    }
}
