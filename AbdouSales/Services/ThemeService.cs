using System;
using System.Linq;
using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// يتحكم في مظهر التطبيق (فاتح / داكن) في وقت التشغيل بنفس أسلوب LocalizationService:
    /// يبدّل قاموس ألوان (Theme.Light.xaml / Theme.Dark.xaml) فتتحدّث كل النوافذ المفتوحة فوراً،
    /// لأن كل الشاشات تستعمل {DynamicResource} لمفاتيح الألوان (BrushBackground, BrushSurface ...).
    /// </summary>
    public static class ThemeService
    {
        public const string Light = "Light";
        public const string Dark = "Dark";

        public static string CurrentTheme { get; private set; } = Light;

        public static bool IsDark => CurrentTheme == Dark;

        /// <summary>يُستدعى عند الإقلاع (قبل فتح أي نافذة) لتطبيق المظهر المحفوظ دون إعادة حفظه.</summary>
        public static void Initialize(string? savedTheme) => Apply(savedTheme, save: false);

        /// <summary>يبدّل المظهر فوراً ويحفظ الاختيار في إعدادات التطبيق.</summary>
        public static void Apply(string? theme, bool save = true)
        {
            theme = string.Equals(theme, Dark, StringComparison.OrdinalIgnoreCase) ? Dark : Light; // أي قيمة غير معروفة = فاتح

            var app = Application.Current;
            if (app == null) return;

            string fileName = theme == Dark ? "Theme.Dark.xaml" : "Theme.Light.xaml";
            var uri = new Uri($"pack://application:,,,/AbdouSales;component/Resources/{fileName}", UriKind.Absolute);
            var newDict = new ResourceDictionary { Source = uri };

            // نزيل أي قاموس مظهر سابق (نتعرّف عليه عبر المفتاح AppThemeName) قبل إضافة الجديد.
            var old = app.Resources.MergedDictionaries.Where(d => d.Contains("AppThemeName")).ToList();
            foreach (var d in old) app.Resources.MergedDictionaries.Remove(d);

            app.Resources.MergedDictionaries.Add(newDict);
            CurrentTheme = theme;

            if (save)
            {
                DataStore.Company.Theme = theme;
                DataStore.SaveCompany();
            }
        }
    }
}
