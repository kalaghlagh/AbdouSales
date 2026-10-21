using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// إظهار/إخفاء الأزرار والخيارات في نوافذ البرنامج حسب اختيار المستخدم في شاشة الإعدادات.
    /// الاستعمال في XAML:  svc:UiVisibility.Key="AddProduct.Save_Click"  على العنصر المراد التحكم فيه،
    /// ثم تستدعي كل نافذة UiVisibility.Apply(this) بعد InitializeComponent. المفاتيح المخفية تُحفظ في
    /// إعدادات الشركة (CompanyInfo.HiddenElements) وقائمة العناصر المتاحة للتحكم في UiCatalog.
    /// </summary>
    public static class UiVisibility
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key", typeof(string), typeof(UiVisibility), new PropertyMetadata(null));

        public static string? GetKey(DependencyObject obj) => (string?)obj.GetValue(KeyProperty);
        public static void SetKey(DependencyObject obj, string? value) => obj.SetValue(KeyProperty, value);

        private static List<string> Hidden => DataStore.Company.HiddenElements ??= new List<string>();

        public static bool IsHidden(string key) => Hidden.Contains(key);

        /// <summary>يغيّر حالة عنصر (دون حفظ في الملف؛ استدعِ Save بعد الانتهاء).</summary>
        public static void SetHidden(string key, bool hidden)
        {
            if (hidden) { if (!Hidden.Contains(key)) Hidden.Add(key); }
            else Hidden.Remove(key);
        }

        public static void ShowAll() => Hidden.Clear();

        public static void Save() => DataStore.SaveCompany();

        /// <summary>يطبّق الإخفاء/الإظهار على كل العناصر الموسومة بـ UiVisibility.Key داخل الجذر (نافذة أو لوحة).</summary>
        public static void Apply(DependencyObject root)
        {
            if (root is FrameworkElement element)
            {
                var key = GetKey(root);
                if (key != null)
                    element.Visibility = IsHidden(key) ? Visibility.Collapsed : Visibility.Visible;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
                Apply(child);
        }
    }
}
