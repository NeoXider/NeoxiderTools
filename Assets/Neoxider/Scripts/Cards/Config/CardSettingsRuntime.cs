namespace Neo.Cards
{
    /// <summary>
    ///     Глобальный runtime-контекст настроек карточной системы.
    /// </summary>
    public static class CardSettingsRuntime
    {
        /// <summary>
        ///     Глобальный конфиг анимаций для компонентов, у которых не задан локальный источник.
        /// </summary>
        public static CardAnimationConfig GlobalAnimationConfig { get; set; }
    }
}
