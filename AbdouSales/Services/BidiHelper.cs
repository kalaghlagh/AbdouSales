using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace AbdouSales.Services
{
    /// <summary>
    /// عرض القيم الرمزية المختلطة (سجل تجاري مثل 21/أ/1236438-00/11، رقم جبائي...) بالترتيب البصري نفسه الذي كُتبت به.
    /// المشكلة: خوارزمية الاتجاه (Unicode Bidi) تعامل الأرقام التي تلي حرفاً عربياً كأنها جزء من كتلة يمين←يسار،
    /// فتنقلب أجزاء القيمة (تظهر 21/11/00-1236438/أ). الحل: عند العرض فقط نحيط الحرف العربي بعلامة اتجاه غير مرئية
    /// (LRM = U+200E). هذه العلامة لا تُحفظ أبداً: القيمة المخزنة في الملفات وقاعدة البيانات تبقى كما أدخلها المستخدم تماماً.
    /// </summary>
    public static class BidiHelper
    {
        public const char Lrm = '\u200E';

        private static bool IsRtlLetter(char c) =>
            (c >= '\u0590' && c <= '\u08FF') || (c >= '\uFB1D' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');

        /// <summary>يزيل علامات الاتجاه من نص الواجهة ويقصّ الطرفين: هذه هي القيمة التي تُحفظ.</summary>
        public static string Clean(string? text) => (text ?? string.Empty).Replace(Lrm.ToString(), string.Empty).Trim();

        /// <summary>نص للعرض فقط: يضع LRM حول كل مقطع عربي ليبقى ترتيب القيمة بصرياً كما كُتب. لا يغيّر أي حرف آخر.</summary>
        public static string Format(string? value)
        {
            var text = (value ?? string.Empty).Replace(Lrm.ToString(), string.Empty);
            var sb = new StringBuilder(text.Length + 4);
            bool inRun = false;
            foreach (char ch in text)
            {
                bool rtl = IsRtlLetter(ch);
                if (rtl && !inRun) { sb.Append(Lrm); inRun = true; }
                else if (!rtl && inRun) { sb.Append(Lrm); inRun = false; }
                sb.Append(ch);
            }
            if (inRun) sb.Append(Lrm);
            return sb.ToString();
        }

        /// <summary>عنصر مضمّن في فقرة (بعد التسمية مباشرة) يعرض القيمة بترتيبها الأصلي.</summary>
        public static InlineUIContainer LtrInline(string? value, double fontSize)
        {
            var text = new TextBlock
            {
                Text = Format((value ?? string.Empty).Trim()),
                FontSize = fontSize,
                FlowDirection = FlowDirection.LeftToRight,
                TextWrapping = TextWrapping.NoWrap
            };
            return new InlineUIContainer(text) { BaselineAlignment = BaselineAlignment.Center };
        }
    }

    /// <summary>محوّل أحادي الاتجاه لخلايا الجداول: يعرض القيمة بالترتيب الصحيح دون المساس بالخاصية الأصلية.</summary>
    public sealed class BidiOrderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => BidiHelper.Format(value as string);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// سلوك لحقول الإدخال الرمزية (السجل التجاري): svc:BidiText.KeepOrder="True".
    /// يضيف علامات LRM غير المرئية أثناء الكتابة ليظهر الترتيب كما يكتبه المستخدم، مع الحفاظ على موضع المؤشر.
    /// عند الحفظ استعمل BidiHelper.Clean(box.Text) فتُزال العلامات وتُحفظ القيمة الأصلية دون أي تغيير.
    /// </summary>
    public static class BidiText
    {
        public static readonly DependencyProperty KeepOrderProperty =
            DependencyProperty.RegisterAttached("KeepOrder", typeof(bool), typeof(BidiText), new PropertyMetadata(false, OnKeepOrderChanged));

        public static bool GetKeepOrder(DependencyObject obj) => (bool)obj.GetValue(KeepOrderProperty);
        public static void SetKeepOrder(DependencyObject obj, bool value) => obj.SetValue(KeepOrderProperty, value);

        private static bool _busy;

        private static void OnKeepOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox box) return;
            box.TextChanged -= Box_TextChanged;
            if ((bool)e.NewValue) box.TextChanged += Box_TextChanged;
        }

        private static void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_busy || sender is not TextBox box) return;

            string raw = box.Text;
            string formatted = BidiHelper.Format(raw);
            if (formatted == raw) return;

            // عدد المحارف الحقيقية (غير LRM) قبل المؤشر، لنعيد المؤشر إلى المكان نفسه بعد إعادة التنسيق
            int caret = Math.Min(box.CaretIndex, raw.Length);
            int realBefore = raw.Take(caret).Count(c => c != BidiHelper.Lrm);

            _busy = true;
            try
            {
                box.Text = formatted;
                int pos = 0, count = 0;
                while (pos < formatted.Length && count < realBefore)
                {
                    if (formatted[pos] != BidiHelper.Lrm) count++;
                    pos++;
                }
                box.CaretIndex = pos;
            }
            finally { _busy = false; }
        }
    }
}
