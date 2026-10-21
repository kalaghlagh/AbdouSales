using System.Windows;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class InvoicePrintWindow : Window
    {
        private readonly Invoice _invoice;

        public InvoicePrintWindow(Invoice invoice)
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            _invoice = invoice;
            RefreshDocument();
        }

        private void RefreshDocument()
        {
            Viewer.Document = InvoiceDocumentBuilder.Build(_invoice, DataStore.Company);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintHelper.Print(Viewer.Document, $"فاتورة رقم {_invoice.Number}");
        }

        /// <summary>يفتح صفحة اختيار قالب الفاتورة، ويطبّق الاختيار فوراً على المعاينة.</summary>
        private void ChangeTemplate_Click(object sender, RoutedEventArgs e)
        {
            var currentStyle = _invoice.TemplateStyle ?? DataStore.Company.DefaultTemplate;
            var chooser = new TemplateChooserWindow(currentStyle, defaultToSaveAsDefault: false) { Owner = this };
            if (chooser.ShowDialog() == true)
            {
                _invoice.TemplateStyle = chooser.SelectedStyle;
                if (chooser.SaveAsDefault)
                {
                    DataStore.Company.DefaultTemplate = chooser.SelectedStyle;
                    DataStore.SaveCompany();
                }
                RefreshDocument();
            }
        }

        /// <summary>يفتح نافذة مشاركة الفاتورة بصيغة PDF عبر واتساب أو جيميل.</summary>
        private void Share_Click(object sender, RoutedEventArgs e)
        {
            new ShareInvoiceWindow(_invoice) { Owner = this }.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
