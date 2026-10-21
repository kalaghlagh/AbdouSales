using System;
using System.Windows;
using System.Windows.Controls;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class InvoiceListWindow : Window
    {
        public InvoiceListWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            InvoicesGrid.ItemsSource = DataStore.Invoices;
        }

        /// <summary>يحفظ فوراً عند تبديل خانة "مدفوعة" من الجدول مباشرة.</summary>
        private void InvoicesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(DataStore.SaveInvoices));
        }

        private void PrintSelected_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is Invoice invoice)
            {
                new InvoicePrintWindow(invoice).ShowDialog();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار فاتورة من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>مشاركة الفاتورة المحددة بصيغة PDF عبر واتساب أو جيميل.</summary>
        private void ShareSelected_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is Invoice invoice)
            {
                new ShareInvoiceWindow(invoice) { Owner = this }.ShowDialog();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار فاتورة من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is Invoice invoice)
            {
                var result = MessageBox.Show($"هل تريد حذف الفاتورة رقم \"{invoice.Number}\"؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataStore.Invoices.Remove(invoice);
                    DataStore.SaveInvoices();
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار فاتورة من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
