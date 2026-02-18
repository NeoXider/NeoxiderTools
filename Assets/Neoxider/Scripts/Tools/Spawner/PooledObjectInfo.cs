using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Вспомогательный компонент, который хранит ссылку на пул, которому принадлежит этот объект.
    /// </summary>
    [NeoDoc("Tools/Spawner/PooledObjectInfo.md")]
    [AddComponentMenu("")] // Скрываем из меню компонентов
    public class PooledObjectInfo : MonoBehaviour
    {
        public NeoObjectPool OwnerPool { get; set; }
    }
}