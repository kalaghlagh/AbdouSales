using System;
using System.Linq;
using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// يتحكم في لغة واجهة التطبيق (العربية/الفرنسية) في وقت التشغيل.
    /// يعمل عبر تبديل قاموس موارد (ResourceDictionary) يحوي كل النصوص المترجمة (المفاتيح Sxxx)
    /// بالإضافة إلى اتجاه الواجهة (AppFlowDirection). بما أن كل الشاشات تستعمل {DynamicResource}،
    /// فإن أي نافذة مفتوحة حالياً تتحدث فوراً عند تبديل اللغة، دون الحاجة لإعادة تشغيل البرنامج.
    /// </summary>
    public static class LocalizationService
    {
        public const string Arabic = "ar";
        public const string French = "fr";

        public static string CurrentLanguage { get; private set; } = Arabic;

        /// <summary>يُستدعى مرة واحدة عند إقلاع التطبيق (قبل فتح أي نافذة) لتطبيق اللغة المحفوظة.</summary>
        public static void Initialize(string savedLanguage)
        {
            SetLanguage(string.IsNullOrWhiteSpace(savedLanguage) ? Arabic : savedLanguage);
        }

        /// <summary>يبدّل لغة الواجهة فوراً في كل نوافذ التطبيق المفتوحة، ويحفظ الاختيار في إعدادات الشركة.</summary>
        public static void SetLanguage(string languageCode)
        {
            languageCode = languageCode == French ? French : Arabic; // أي قيمة غير معروفة ترجع للعربية

            var app = Application.Current;
            if (app == null) return;

            string fileName = languageCode == French ? "Lang.fr.xaml" : "Lang.ar.xaml";
            var uri = new Uri($"pack://application:,,,/AbdouSales;component/Resources/{fileName}", UriKind.Absolute);
            var newDict = new ResourceDictionary { Source = uri };

            // نزيل أي قاموس لغة سابق (نتعرّف عليه عبر وجود مفتاح AppFlowDirection بداخله) قبل إضافة الجديد.
            var oldDicts = app.Resources.MergedDictionaries
                .Where(d => d.Contains("AppFlowDirection"))
                .ToList();
            foreach (var old in oldDicts) app.Resources.MergedDictionaries.Remove(old);

            app.Resources.MergedDictionaries.Add(newDict);
            CurrentLanguage = languageCode;

            DataStore.Company.Language = languageCode;
            DataStore.SaveCompany();
        }

        /// <summary>يعيد النص المترجم لمفتاح معيّن؛ مفيد داخل الكود (مثلاً نصوص MessageBox) حيث لا يمكن استعمال DynamicResource مباشرة.</summary>
        public static string Get(string key)
        {
            var app = Application.Current;
            if (app?.TryFindResource(key) is string s) return s;
            return key;
        }
    }
}
