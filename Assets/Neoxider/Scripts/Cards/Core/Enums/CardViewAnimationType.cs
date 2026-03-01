namespace Neo.Cards
{
    /// <summary>
    ///     Типы готовых анимаций карты для CardViewAnimationTemplates и ICardViewAnimations.
    /// </summary>
    public enum CardViewAnimationType
    {
        /// <summary>Упругий масштаб (DOPunchScale).</summary>
        Bounce,

        /// <summary>Один цикл пульсации масштаба.</summary>
        Pulse,

        /// <summary>Зацикленная пульсация масштаба.</summary>
        PulseLooped,

        /// <summary>Тряска позиции (DOShakePosition).</summary>
        Shake,

        /// <summary>Краткое подсвечивание (цвет/alpha).</summary>
        Highlight,

        /// <summary>Влёт + появление (движение + scale/alpha).</summary>
        FlyIn,

        /// <summary>Лёгкое зацикленное покачивание.</summary>
        Idle
    }
}