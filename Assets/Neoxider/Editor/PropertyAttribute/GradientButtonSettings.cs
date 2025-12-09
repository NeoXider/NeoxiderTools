using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Настройки визуального оформления градиентных кнопок Neo ButtonAttribute
    /// </summary>
    public static class GradientButtonSettings
    {
        // === ЦВЕТОВЫЕ СХЕМЫ (выберите одну) ===

        // Схема 1: Неоновый киберпанк (Cyan -> Purple)
        private static readonly Color Scheme1_Top = new(0.2f, 0.8f, 1f, 1f); // Яркий cyan
        private static readonly Color Scheme1_Bottom = new(0.6f, 0.2f, 1f, 1f); // Фиолетовый

        // Схема 2: Глубокий элегантный (Dark Blue -> Purple)
        private static readonly Color Scheme2_Top = new(0.3f, 0.4f, 0.9f, 1f); // Синий
        private static readonly Color Scheme2_Bottom = new(0.7f, 0.3f, 0.9f, 1f); // Фиолетовый

        // Схема 3: Discord стиль (Blurple gradient)
        private static readonly Color Scheme3_Top = new(0.35f, 0.4f, 0.95f, 1f); // Blurple светлый
        private static readonly Color Scheme3_Bottom = new(0.45f, 0.3f, 0.85f, 1f); // Blurple темный

        // Схема 4: Тёплый закат (Orange -> Pink)
        private static readonly Color Scheme4_Top = new(1f, 0.45f, 0.3f, 1f); // Оранжевый
        private static readonly Color Scheme4_Bottom = new(0.95f, 0.3f, 0.6f, 1f); // Розовый

        // Схема 5: Зелёный Matrix (Green gradient)
        private static readonly Color Scheme5_Top = new(0.3f, 0.95f, 0.5f, 1f); // Светло-зелёный
        private static readonly Color Scheme5_Bottom = new(0.2f, 0.7f, 0.4f, 1f); // Тёмно-зелёный

        // Схема 6: Тёмный киберпанк (Dark Cyber) ⭐ НОВАЯ
        private static readonly Color Scheme6_Top = new(0.15f, 0.15f, 0.25f, 1f); // Тёмно-синий
        private static readonly Color Scheme6_Bottom = new(0.25f, 0.1f, 0.35f, 1f); // Тёмно-фиолетовый
        private static readonly Color Scheme6_Accent = new(0.4f, 0.9f, 1f, 1f); // Неоновый cyan акцент

        // === АКТИВНАЯ СХЕМА (измените номер для выбора) ===
        private static readonly int ActiveScheme = 6; // 1-6

        // Цвета градиента кнопки (автоматически из выбранной схемы)
        public static Color TopColor => ActiveScheme switch
        {
            1 => Scheme1_Top,
            2 => Scheme2_Top,
            3 => Scheme3_Top,
            4 => Scheme4_Top,
            5 => Scheme5_Top,
            6 => Scheme6_Top,
            _ => Scheme6_Top
        };

        public static Color BottomColor => ActiveScheme switch
        {
            1 => Scheme1_Bottom,
            2 => Scheme2_Bottom,
            3 => Scheme3_Bottom,
            4 => Scheme4_Bottom,
            5 => Scheme5_Bottom,
            6 => Scheme6_Bottom,
            _ => Scheme6_Bottom
        };

        // Акцентный цвет (для киберпанк эффектов)
        public static Color AccentColor => ActiveScheme == 6 ? Scheme6_Accent : new Color(1f, 1f, 1f, 0.3f);

        // Hover эффект
        public static float HoverBrightness => 0.15f;

        // Размеры
        public static float DefaultButtonHeight => 22f;
        public static float CornerRadius => 6f;
        public static float ButtonSpacing => 4f; // Отступ между кнопками

        // Градиент
        public static int GradientSegments => 20;

        // Закруглённые углы (увеличено для большей точности)
        public static int CornerMaskSteps => 16;

        // Тень/обводка (для киберпанк схемы используем неоновый цвет)
        public static Color HighlightColor => ActiveScheme == 6 ? AccentColor : new Color(1f, 1f, 1f, 0.3f);
        public static float HighlightWidth => 2f;

        // Неоновое свечение для киберпанк схемы
        public static bool EnableNeonGlow => ActiveScheme == 6;
        public static Color NeonGlowColor => AccentColor;

        // Текст
        public static Color TextColor => Color.white;
        public static FontStyle TextStyle => FontStyle.Bold;
        public static TextAnchor TextAlignment => TextAnchor.MiddleCenter;

        // Цвет фона Unity инспектора (для маски углов)
        public static Color InspectorBackgroundColor => new(0.22f, 0.22f, 0.22f, 1f);
    }
}