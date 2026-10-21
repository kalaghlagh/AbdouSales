using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    /// <summary>
    /// شاشة نقطة البيع (الكاشير): تسمح بمسح رموز السلع بقارئ باركود (يعمل كلوحة مفاتيح USB/بلوتوث
    /// عادية - لا يحتاج أي تعريف خاص)، أو البحث اليدوي عن السلعة، ثم إتمام عملية البيع بسرعة
    /// وطباعة/حفظ الفاتورة الناتجة مباشرة.
    /// </summary>
    public partial class CashierWindow : Window
    {
        private readonly ObservableCollection<InvoiceLine> _cart = new();

        public CashierWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم

            ProductPickerCombo.ItemsSource = DataStore.Products;
            CartGrid.ItemsSource = _cart;
            _cart.CollectionChanged += Cart_CollectionChanged;

            RecalculateTotals();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => BarcodeBox.Focus();

        private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (InvoiceLine line in e.NewItems) line.PropertyChanged += Line_PropertyChanged;
            if (e.OldItems != null)
                foreach (InvoiceLine line in e.OldItems) line.PropertyChanged -= Line_PropertyChanged;
            RecalculateTotals();
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

        // ===================== المسح / البحث =====================

        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string code = BarcodeBox.Text.Trim();
            BarcodeBox.Clear();
            if (string.IsNullOrWhiteSpace(code)) return;

            var product = DataStore.FindProductByBarcode(code);
            if (product == null)
            {
                var result = MessageBox.Show(
                    $"لا توجد سلعة بهذا الرمز الشريطي:\n{code}\n\nهل تريد إضافة سلعة جديدة بهذا الرمز الآن؟",
                    "لم يتم العثور على السلعة", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var addWindow = new AddProductWindow { Owner = this };
                    addWindow.ShowDialog();
                    // بعد الإضافة اليدوية نعيد محاولة المسح بنفس الرمز.
                    product = DataStore.FindProductByBarcode(code);
                }

                if (product == null)
                {
                    BarcodeBox.Focus();
                    return;
                }
            }

            AddOrIncrementLine(product);
            BarcodeBox.Focus();
        }

        private void ProductPickerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductPickerCombo.SelectedItem is Product p)
            {
                AddOrIncrementLine(p);
                ProductPickerCombo.SelectedItem = null;
                BarcodeBox.Focus();
            }
        }

        private void AddOrIncrementLine(Product product)
        {
            decimal.TryParse(ScanQuantityBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var scanQty);
            if (scanQty <= 0) scanQty = 1;

            var existing = _cart.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += scanQty;
            }
            else
            {
                _cart.Add(new InvoiceLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    Quantity = scanQty,
                    UnitPrice = product.UnitPrice,
                    CostPrice = product.CostPrice
                });
            }
        }

        private void FocusBarcode_Click(object sender, RoutedEventArgs e) => BarcodeBox.Focus();

        // ===================== السلة =====================

        private void RemoveCartLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is InvoiceLine line)
            {
                _cart.Remove(line);
            }
        }

        private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RecalculateTotals));
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;
            if (MessageBox.Show("هل تريد إفراغ السلة الحالية؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _cart.Clear();
                AmountReceivedBox.Text = "";
                CustomerNameBox.Text = "زبون عابر";
                BarcodeBox.Focus();
            }
        }

        // ===================== المجاميع =====================

        private void Totals_Changed(object sender, TextChangedEventArgs e) => RecalculateTotals();

        private void RecalculateTotals()
        {
            if (SubTotalText == null) return;

            decimal subTotal = _cart.Sum(l => l.Total);
            decimal.TryParse(VatRateBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRate);
            decimal vatAmount = Math.Round(subTotal * vatRate / 100m, 2);
            decimal grandTotal = Math.Round(subTotal + vatAmount, 2);

            SubTotalText.Text = $"{subTotal:N2} د.ج";
            VatText.Text = $"{vatAmount:N2} د.ج";
            GrandTotalText.Text = $"{grandTotal:N2} د.ج";

            ItemCountText.Text = $"{_cart.Count} سلعة — {_cart.Sum(l => l.Quantity):0.##} قطعة";

            decimal.TryParse(AmountReceivedBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var received);
            decimal change = received - grandTotal;
            if (ChangeText != null)
            {
                ChangeText.Text = $"{Math.Max(change, 0):N2} د.ج";
                ChangeText.Foreground = change < 0
                    ? System.Windows.Media.Brushes.IndianRed
                    : (System.Windows.Media.Brush)FindResource("BrushGreen");
            }
        }

        // ===================== إتمام البيع =====================

        private bool TryBuildInvoice(out Invoice invoice)
        {
            invoice = new Invoice();

            if (_cart.Count == 0)
            {
                MessageBox.Show("السلة فارغة. الرجاء مسح أو اختيار سلعة واحدة على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            decimal.TryParse(VatRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRate);
            decimal.TryParse(AmountReceivedBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var received);

            decimal subTotal = _cart.Sum(l => l.Total);
            decimal vatAmount = Math.Round(subTotal * vatRate / 100m, 2);
            decimal grandTotal = Math.Round(subTotal + vatAmount, 2);

            invoice.Number = DataStore.GetNextCashierInvoiceNumber();
            invoice.Date = DateTime.Now;
            invoice.CustomerName = string.IsNullOrWhiteSpace(CustomerNameBox.Text) ? "زبون عابر" : CustomerNameBox.Text.Trim();
            invoice.VatRate = vatRate;
            invoice.PaymentMethod = (PaymentMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "نقدا";
            invoice.Lines = _cart.ToList();
            invoice.IsCashierSale = true;
            invoice.IsPaid = true; // مبيعات الكاشير تُدفع فوراً دائماً
            invoice.AmountReceived = received;
            invoice.ChangeGiven = Math.Max(received - grandTotal, 0);
            invoice.TemplateStyle = DataStore.Company.DefaultTemplate;

            return true;
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildInvoice(out var invoice)) return;

            DataStore.Invoices.Add(invoice);
            DataStore.SaveInvoices();
            DataStore.DeductStockForInvoice(invoice);

            var preview = new InvoicePrintWindow(invoice) { Owner = this };
            preview.ShowDialog();

            ResetSaleScreen();
        }

        private void SaveOnly_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildInvoice(out var invoice)) return;

            DataStore.Invoices.Add(invoice);
            DataStore.SaveInvoices();
            DataStore.DeductStockForInvoice(invoice);

            MessageBox.Show($"تم حفظ عملية البيع رقم {invoice.Number} بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetSaleScreen();
        }

        private void ResetSaleScreen()
        {
            _cart.Clear();
            AmountReceivedBox.Text = "";
            CustomerNameBox.Text = "زبون عابر";
            BarcodeBox.Focus();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
