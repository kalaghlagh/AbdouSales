using System;
using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// يجعل أي نافذة تتلاءم مع شاشة الحاسوب المحمول (مثل 14 بوصة: 1366×768 أو 1920×1080 بمقياس 125% / 150%).
    /// تُستدعى مرة واحدة في مُنشئ النافذة بعد InitializeComponent. لا تُكبّر النافذة أبداً، بل تُصغّر
    /// الحجم والحد الأدنى فقط إذا تجاوزا المساحة المتاحة (الشاشة بعد استبعاد شريط المهام).
    /// </summary>
    public static class ScreenFit
    {
        /// <summary>هامش صغير يُترك حول النافذة حتى لا تلتصق بحواف الشاشة.</summary>
        private const double Margin = 8;

        public static void Apply(Window window)
        {
            Rect area = SystemParameters.WorkArea; // بوحدات WPF (مستقلة عن DPI) وتستثني شريط المهام
            double maxWidth = Math.Max(320, area.Width - Margin);
            double maxHeight = Math.Max(240, area.Height - Margin);

            if (!double.IsNaN(window.MinWidth)) window.MinWidth = Math.Min(window.MinWidth, maxWidth);
            if (!double.IsNaN(window.MinHeight)) window.MinHeight = Math.Min(window.MinHeight, maxHeight);

            if (!double.IsNaN(window.Width)) window.Width = Math.Min(window.Width, maxWidth);
            if (!double.IsNaN(window.Height)) window.Height = Math.Min(window.Height, maxHeight);
        }
    }
}
