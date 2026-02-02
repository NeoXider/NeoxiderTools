using System;

namespace Neo.Save
{
    /// <summary>
    ///     Интерфейс для провайдеров сохранения данных.
    ///     Предоставляет унифицированный API для работы с различными системами сохранения.
    /// </summary>
    public interface ISaveProvider
    {
        /// <summary>
        ///     Тип провайдера.
        /// </summary>
        SaveProviderType ProviderType { get; }

        /// <summary>
        ///     Событие, вызываемое после сохранения данных.
        /// </summary>
        event Action OnDataSaved;

        /// <summary>
        ///     Событие, вызываемое после загрузки данных.
        /// </summary>
        event Action OnDataLoaded;

        /// <summary>
        ///     Событие, вызываемое при изменении значения ключа.
        /// </summary>
        /// <param name="key">Ключ, значение которого изменилось</param>
        event Action<string> OnKeyChanged;

        /// <summary>
        ///     Получает целочисленное значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для получения значения</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Значение по ключу или значение по умолчанию</returns>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>
        ///     Устанавливает целочисленное значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для установки значения</param>
        /// <param name="value">Значение для сохранения</param>
        void SetInt(string key, int value);

        /// <summary>
        ///     Получает значение с плавающей точкой по ключу.
        /// </summary>
        /// <param name="key">Ключ для получения значения</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Значение по ключу или значение по умолчанию</returns>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>
        ///     Устанавливает значение с плавающей точкой по ключу.
        /// </summary>
        /// <param name="key">Ключ для установки значения</param>
        /// <param name="value">Значение для сохранения</param>
        void SetFloat(string key, float value);

        /// <summary>
        ///     Получает строковое значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для получения значения</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Значение по ключу или значение по умолчанию</returns>
        string GetString(string key, string defaultValue = "");

        /// <summary>
        ///     Устанавливает строковое значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для установки значения</param>
        /// <param name="value">Значение для сохранения</param>
        void SetString(string key, string value);

        /// <summary>
        ///     Получает булево значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для получения значения</param>
        /// <param name="defaultValue">Значение по умолчанию, если ключ не существует</param>
        /// <returns>Значение по ключу или значение по умолчанию</returns>
        bool GetBool(string key, bool defaultValue = false);

        /// <summary>
        ///     Устанавливает булево значение по ключу.
        /// </summary>
        /// <param name="key">Ключ для установки значения</param>
        /// <param name="value">Значение для сохранения</param>
        void SetBool(string key, bool value);

        /// <summary>
        ///     Проверяет, существует ли ключ в хранилище.
        /// </summary>
        /// <param name="key">Ключ для проверки</param>
        /// <returns>true, если ключ существует, иначе false</returns>
        bool HasKey(string key);

        /// <summary>
        ///     Удаляет ключ и его значение из хранилища.
        /// </summary>
        /// <param name="key">Ключ для удаления</param>
        void DeleteKey(string key);

        /// <summary>
        ///     Удаляет все ключи из хранилища.
        /// </summary>
        void DeleteAll();

        /// <summary>
        ///     Принудительно сохраняет данные в хранилище.
        /// </summary>
        void Save();

        /// <summary>
        ///     Принудительно загружает данные из хранилища.
        /// </summary>
        void Load();
    }
}


