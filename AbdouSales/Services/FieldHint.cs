using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// خاصية مرتبطة (Attached) تعرض نصاً إرشادياً باهتاً داخل حقل TextBox الفارغ، مثل: "مثال: 21/A/123456-00/11".
    /// يختفي النص بمجرد الكتابة. الاستعمال: svc:FieldHint.Text="{DynamicResource S170}"
    /// </summary>
    public static class FieldHint
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(FieldHint), new PropertyMetadata(string.Empty));

        public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
        public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);
    }
}
