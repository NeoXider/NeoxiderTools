using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Вспомогательный компонент, который хранит ссылку на пул, которому принадлежит этот объект.
    ///     Добавляется автоматически при создании объекта пулом. Для возврата в пул используйте Return() или PoolManager.Release.
    /// </summary>
    [NeoDoc("Tools/Spawner/PooledObjectInfo.md")]
    [AddComponentMenu("")] // Скрываем из меню компонентов
    public class PooledObjectInfo : MonoBehaviour
    {
        public NeoObjectPool OwnerPool { get; set; }

        /// <summary>Возвращает объект в пул. Эквивалентно PoolManager.Release(gameObject).</summary>
        [Button("Return to pool")]
        public void Return()
        {
            if (OwnerPool != null)
                OwnerPool.ReturnObject(gameObject);
            else
                Destroy(gameObject);
        }
    }
}