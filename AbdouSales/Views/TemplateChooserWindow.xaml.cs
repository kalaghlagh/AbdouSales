using AbdouSales.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AbdouSales.Models;

namespace AbdouSales.Views
{
    /// <summary>
    /// صفحة اختيار قالب الفاتورة. تُستعمل بشكل مستقل من القائمة الرئيسية (لتحديد القالب الافتراضي
    /// للشركة)، أو من نافذة معاينة الفاتورة (لتغيير قالب فاتورة معيّنة قبل الطباعة أو المشاركة).
    /// </summary>
    public partial class TemplateChooserWindow : Window
    {
        private static readonly Brush SelectedGlow = new SolidColorBrush(Color.FromRgb(52, 168, 83));

        public InvoiceTemplateStyle SelectedStyle { get; private set; }
        public bool SaveAsDefault { get; private set; }

        private readonly Brush _classicIdleBorder;
        private readonly Brush _navyIdleBorder;
        private readonly Brush _royalIdleBorder;

        public TemplateChooserWindow(InvoiceTemplateStyle currentStyle, bool defaultToSaveAsDefault = true)
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم

            _classicIdleBorder = ClassicCard.BorderBrush;
            _navyIdleBorder = NavyCard.BorderBrush;
            _royalIdleBorder = RoyalCard.BorderBrush;

            SelectedStyle = currentStyle;
            SetAsDefaultCheck.IsChecked = defaultToSaveAsDefault;

            HighlightSelected();
        }

        private void HighlightSelected()
        {
            ClassicCard.BorderBrush = _classicIdleBorder;
            NavyCard.BorderBrush = _navyIdleBorder;
            RoyalCard.BorderBrush = _royalIdleBorder;
            ClassicCard.BorderThickness = new Thickness(2);
            NavyCard.BorderThickness = new Thickness(2);
            RoyalCard.BorderThickness = new Thickness(2);

            switch (SelectedStyle)
            {
                case InvoiceTemplateStyle.ClassicClean:
                    ClassicCard.BorderBrush = SelectedGlow;
                    ClassicCard.BorderThickness = new Thickness(3.5);
                    break;
                case InvoiceTemplateStyle.ModernRoyalBlue:
                    RoyalCard.BorderBrush = SelectedGlow;
                    RoyalCard.BorderThickness = new Thickness(3.5);
                    break;
                default:
                    NavyCard.BorderBrush = SelectedGlow;
                    NavyCard.BorderThickness = new Thickness(3.5);
                    break;
            }
        }

        private void ClassicCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedStyle = InvoiceTemplateStyle.ClassicClean;
            HighlightSelected();
        }

        private void NavyCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedStyle = InvoiceTemplateStyle.ExecutiveNavy;
            HighlightSelected();
        }

        private void RoyalCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedStyle = InvoiceTemplateStyle.ModernRoyalBlue;
            HighlightSelected();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            SaveAsDefault = SetAsDefaultCheck.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
