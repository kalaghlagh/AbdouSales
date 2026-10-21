using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    /// <summary>عرض مساعد للسلعة لإظهار قيمة المخزون (الكمية × السعر) بدون تعديل نموذج Product نفسه.</summary>
    public class StockRow
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal StockValue => Math.Round(QuantityInStock * UnitPrice, 2);
    }

    /// <summary>
    /// لوحة المعلومات: إجمالي المبيعات والأرباح، عدد ومبلغ الفواتير غير المدفوعة، السلع منخفضة
    /// المخزون، وقائمة كاملة بكميات كل سلعة في المخزون. كل الأرقام تُحسب مباشرة من البيانات
    /// المحفوظة (لا حاجة لأي إعداد إضافي).
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private const decimal LowStockThreshold = 5m;

        public DashboardWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            Refresh();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

        private void Refresh()
        {
            var invoices = DataStore.Invoices;
            var products = DataStore.Products;

            decimal revenue = invoices.Sum(i => i.GrandTotal);
            decimal profit = invoices.Sum(i => i.Profit);
            var unpaid = invoices.Where(i => !i.IsPaid).OrderByDescending(i => i.Date).ToList();
            decimal unpaidTotal = unpaid.Sum(i => i.GrandTotal);
            int lowStockCount = products.Count(p => p.QuantityInStock <= LowStockThreshold);

            RevenueText.Text = $"{revenue:N2} د.ج";
            ProfitText.Text = $"{profit:N2} د.ج";
            UnpaidCountText.Text = unpaid.Count.ToString(CultureInfo.InvariantCulture);
            UnpaidAmountText.Text = $"{unpaidTotal:N2} د.ج";
            LowStockCountText.Text = lowStockCount.ToString(CultureInfo.InvariantCulture);

            StockGrid.ItemsSource = products
                .OrderBy(p => p.Name)
                .Select(p => new StockRow
                {
                    Name = p.Name,
                    Unit = p.Unit,
                    QuantityInStock = p.QuantityInStock,
                    UnitPrice = p.UnitPrice
                })
                .ToList();

            UnpaidGrid.ItemsSource = unpaid;
            bool hasUnpaid = unpaid.Count > 0;
            UnpaidGrid.Visibility = hasUnpaid ? Visibility.Visible : Visibility.Collapsed;
            NoUnpaidText.Visibility = hasUnpaid ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>يعلّم الفاتورة المحددة كمدفوعة، ويحدّث اللوحة فوراً.</summary>
        private void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Invoice invoice)
            {
                invoice.IsPaid = true;
                DataStore.SaveInvoices();
                Refresh();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
