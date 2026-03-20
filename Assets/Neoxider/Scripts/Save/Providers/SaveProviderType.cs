namespace Neo.Save
{
    /// <summary>
    ///     Тип провайдера для системы сохранения данных.
    /// </summary>
    public enum SaveProviderType
    {
        /// <summary>
        ///     Сохранение через PlayerPrefs Unity (по умолчанию).
        /// </summary>
        PlayerPrefs,

        /// <summary>
        ///     Сохранение в JSON файл.
        /// </summary>
        File
    }
}
