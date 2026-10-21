using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    /// <summary>
    /// نافذة مشاركة الفاتورة: تنشئ ملف PDF ثم ترسله عبر واتساب أو جيميل.
    /// </summary>
    public partial class ShareInvoiceWindow : Window
    {
        private readonly Invoice _invoice;
        private string _pdfPath = string.Empty;

        public ShareInvoiceWindow(Invoice invoice)
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            _invoice = invoice;

            // نستخرج هاتف الزبون من بطاقة الزبون إن وُجدت لتعبئة الحقل تلقائياً.
            var customer = DataStore.Customers.FirstOrDefault(c => c.Id == invoice.CustomerId)
                           ?? DataStore.Customers.FirstOrDefault(c =>
                                  string.Equals(c.Name.Trim(), invoice.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (customer != null) PhoneBox.Text = customer.Phone;

            Loaded += ShareInvoiceWindow_Loaded;
        }

        private void ShareInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePdf();
        }

        /// <summary>ينشئ ملف PDF للفاتورة ويعرض مساره.</summary>
        private bool GeneratePdf()
        {
            if (!string.IsNullOrEmpty(_pdfPath) && File.Exists(_pdfPath)) return true;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _pdfPath = PdfExporter.ExportInvoice(_invoice, DataStore.Company);
                PathText.Text = _pdfPath;
                return true;
            }
            catch (Exception ex)
            {
                PathText.Text = "تعذر إنشاء ملف PDF.";
                MessageBox.Show(
                    "حدث خطأ أثناء إنشاء ملف PDF للفاتورة:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>نص الرسالة المرافقة للفاتورة.</summary>
        private string BuildMessage()
        {
            string company = string.IsNullOrWhiteSpace(DataStore.Company.Name) ? "AbdouSales" : DataStore.Company.Name;
            string customer = string.IsNullOrWhiteSpace(_invoice.CustomerName) ? "زبوننا الكريم" : _invoice.CustomerName;

            return
                $"السلام عليكم {customer}،\n" +
                $"نرسل لكم فاتورة رقم {_invoice.Number} بتاريخ {_invoice.Date:dd/MM/yyyy}.\n" +
                $"المجموع الكلي: {_invoice.GrandTotal:N2} دج.\n" +
                $"مع تحيات {company}.";
        }

        private string BuildSubject()
        {
            string company = string.IsNullOrWhiteSpace(DataStore.Company.Name) ? "AbdouSales" : DataStore.Company.Name;
            return $"فاتورة رقم {_invoice.Number} - {company}";
        }

        private void WhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (!GeneratePdf()) return;

            bool copied = ShareHelper.CopyFileToClipboard(_pdfPath);
            ShareHelper.OpenWhatsApp(BuildMessage(), PhoneBox.Text);

            MessageBox.Show(
                copied
                    ? "تم فتح واتساب ونسخ ملف الفاتورة.\n\nداخل المحادثة اضغط Ctrl + V لإرفاق الفاتورة ثم أرسلها."
                    : "تم فتح واتساب، لكن تعذر نسخ الملف إلى الحافظة.\n\nيمكنك فتح مجلد الفاتورة وسحب الملف إلى المحادثة.",
                "مشاركة عبر واتساب", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Gmail_Click(object sender, RoutedEventArgs e)
        {
            if (!GeneratePdf()) return;

            bool copied = ShareHelper.CopyFileToClipboard(_pdfPath);
            ShareHelper.OpenGmail(BuildSubject(), BuildMessage(), EmailBox.Text);

            MessageBox.Show(
                copied
                    ? "تم فتح جيميل ونسخ ملف الفاتورة.\n\nداخل الرسالة اضغط Ctrl + V لإرفاق الفاتورة ثم أرسلها."
                    : "تم فتح جيميل، لكن تعذر نسخ الملف إلى الحافظة.\n\nيمكنك فتح مجلد الفاتورة وسحب الملف إلى الرسالة.",
                "مشاركة عبر جيميل", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyOnly_Click(object sender, RoutedEventArgs e)
        {
            if (!GeneratePdf()) return;

            bool copied = ShareHelper.CopyFileToClipboard(_pdfPath);
            MessageBox.Show(
                copied
                    ? "تم نسخ ملف الفاتورة. يمكنك لصقه بـ Ctrl + V في أي تطبيق (واتساب، جيميل، فيسبوك...)."
                    : "تعذر نسخ الملف إلى الحافظة، حاول مرة أخرى.",
                "نسخ الملف", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (GeneratePdf()) ShareHelper.ShowFileInExplorer(_pdfPath);
        }

        private void OpenPdf_Click(object sender, RoutedEventArgs e)
        {
            if (GeneratePdf()) ShareHelper.OpenFile(_pdfPath);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (!GeneratePdf()) return;

            var dialog = new SaveFileDialog
            {
                Filter = "ملف PDF (*.pdf)|*.pdf",
                FileName = PdfExporter.BuildFileName(_invoice),
                Title = "حفظ الفاتورة بصيغة PDF"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_pdfPath, dialog.FileName, true);
                    MessageBox.Show("تم حفظ الفاتورة بنجاح.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("تعذر حفظ الملف:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
