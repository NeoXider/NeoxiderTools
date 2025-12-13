using System;
using System.Linq;

namespace Neo.Save
{
    /// <summary>
    ///     Расширения для работы с провайдерами сохранения.
    ///     Предоставляет дополнительные методы для работы с массивами и коллекциями.
    /// </summary>
    public static class SaveProviderExtensions
    {
        private const char SEPARATOR = ',';

        /// <summary>
        ///     Сохраняет массив целых чисел по ключу.
        /// </summary>
        /// <param name="provider">Провайдер сохранения</param>
        /// <param name="key">Ключ для сохранения</param>
        /// <param name="array">Массив для сохранения</param>
        public static void SetIntArray(this ISaveProvider provider, string key, int[] array)
        {
            if (array == null || array.Length == 0)
            {
                provider.DeleteKey(key);
                return;
            }

            provider.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Загружает массив целых чисел по ключу.
        /// </summary>
        /// <param name="provider">Провайдер сохранения</param>
        /// <param name="key">Ключ для загрузки</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Массив целых чисел</returns>
        public static int[] GetIntArray(this ISaveProvider provider, string key, int[] defaultValue = null)
        {
            if (!provider.HasKey(key))
            {
                return defaultValue ?? new int[0];
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new int[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR).Select(int.Parse).ToArray();
            }
            catch (Exception)
            {
                return defaultValue ?? new int[0];
            }
        }

        /// <summary>
        ///     Сохраняет массив чисел с плавающей точкой по ключу.
        /// </summary>
        /// <param name="provider">Провайдер сохранения</param>
        /// <param name="key">Ключ для сохранения</param>
        /// <param name="array">Массив для сохранения</param>
        public static void SetFloatArray(this ISaveProvider provider, string key, float[] array)
        {
            if (array == null || array.Length == 0)
            {
                provider.DeleteKey(key);
                return;
            }

            provider.SetString(key, string.Join(SEPARATOR.ToString(), array));
        }

        /// <summary>
        ///     Загружает массив чисел с плавающей точкой по ключу.
        /// </summary>
        /// <param name="provider">Провайдер сохранения</param>
        /// <param name="key">Ключ для загрузки</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Массив чисел с плавающей точкой</returns>
        public static float[] GetFloatArray(this ISaveProvider provider, string key, float[] defaultValue = null)
        {
            if (!provider.HasKey(key))
            {
                return defaultValue ?? new float[0];
            }

            string arrayString = provider.GetString(key);
            if (string.IsNullOrEmpty(arrayString))
            {
                return defaultValue ?? new float[0];
            }

            try
            {
                return arrayString.Split(SEPARATOR).Select(float.Parse).ToArray();
            }
            catch (Exception)
            {
                return defaultValue ?? new float[0];
            }
        }
    }
}



