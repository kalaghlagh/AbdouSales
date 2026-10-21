using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class NewInvoiceWindow : Window
    {
        private readonly ObservableCollection<InvoiceLine> _lines = new();
        private Customer? _selectedCustomer;
        private Supplier? _selectedSupplier;

        /// <summary>سلع الممون المختار فقط (من جدول SupplierProducts)؛ هي مصدر قائمة السلع في الفاتورة.</summary>
        private readonly ObservableCollection<Product> _compatibleProducts = new();
        private bool _suppressSupplierChange; // لتفادي إعادة الدخول عند إرجاع الممون السابق

        // آخر رقم وُلّد تلقائياً: إن عدّل المستخدم الرقم يدوياً لا نستبدله عند تغيير الممون أو التاريخ
        private string _lastAutoNumber = string.Empty;

        /// <summary>
        /// ترقيم الفواتير مرتبط بالممون: لكل اسم ممون في الترويسة تسلسله الخاص (2026/001، 2026/002 ...).
        /// يُعاد حساب الرقم عند تغيير اسم الممون أو تاريخ الفاتورة ما لم يكن المستخدم قد غيّره يدوياً.
        /// </summary>
        private void RefreshAutoNumber()
        {
            if (InvoiceNumberBox == null || SupplierNameBox == null || InvoiceDatePicker == null) return;

            string current = InvoiceNumberBox.Text.Trim();
            if (current.Length != 0 && current != _lastAutoNumber) return; // رقم أدخله المستخدم يدوياً

            _lastAutoNumber = DataStore.GetNextInvoiceNumber(SupplierNameBox.Text, InvoiceDatePicker.SelectedDate);
            InvoiceNumberBox.Text = _lastAutoNumber;
        }

        private void SupplierNameBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshAutoNumber();

        private void InvoiceDate_Changed(object? sender, SelectionChangedEventArgs e) => RefreshAutoNumber();

        public NewInvoiceWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم

            InvoiceDatePicker.SelectedDate = DateTime.Now;
            RefreshAutoNumber();

            SupplierCombo.ItemsSource = DataStore.Suppliers;
            CustomerCombo.ItemsSource = DataStore.Customers;
            ProductPickerCombo.ItemsSource = _compatibleProducts;
            RefreshProductList(); // لا ممون مختار بعد: القائمة معطلة وفارغة

            LinesGrid.ItemsSource = _lines;
            _lines.CollectionChanged += Lines_CollectionChanged;

            RecalculateTotals();
        }

        private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InvoiceLine line in e.NewItems)
                    line.PropertyChanged += Line_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (InvoiceLine line in e.OldItems)
                    line.PropertyChanged -= Line_PropertyChanged;
            }
            RecalculateTotals();
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

        private void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerCombo.SelectedItem is Customer c)
            {
                _selectedCustomer = c;
                CustomerNameBox.Text = c.Name;
                CustomerActivityBox.Text = c.Activity;
                CustomerAddressBox.Text = c.Address;
                CustomerCommercialRegisterBox.Text = c.CommercialRegister;
                CustomerTaxIdBox.Text = c.TaxId;
            }
        }

        /// <summary>عند اختيار ممون من القائمة، تُملأ بيانات ترويسة الفاتورة تلقائياً من بياناته (بدل بيانات الشركة الافتراضية)؛ يمكن تعديلها يدوياً بعد ذلك.
        /// كما تُحدَّث قائمة السلع وأسعار الشراء لتعرض فقط سلع الممون الجديد وأسعاره.</summary>
        private void SupplierCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSupplierChange) return;

            if (SupplierCombo.SelectedItem is Supplier s)
            {
                // إن كانت في الفاتورة سطور غير متوافقة مع الممون الجديد: نطلب التأكيد، وعند الرفض نُبقي الممون السابق.
                if (!ConfirmRemoveIncompatibleLines(s))
                {
                    _suppressSupplierChange = true;
                    SupplierCombo.SelectedItem = _selectedSupplier;
                    _suppressSupplierChange = false;
                    return;
                }

                _selectedSupplier = s;
                SupplierNameBox.Text = s.Name;
                SupplierActivityBox.Text = s.Activity;
                SupplierAddressBox.Text = s.Address;
                SupplierCommercialRegisterBox.Text = s.CommercialRegister;
                SupplierTaxIdBox.Text = s.TaxId;
                SupplierArticleNumberBox.Text = s.ArticleNumber;

                RefreshProductList();
                RefreshLinePurchasePrices();
            }
        }

        /// <summary>يعيد بناء قائمة السلع من سلع الممون المختار فقط (SupplierProducts). بدون ممون (أو بدون سلع مرتبطة به) تبقى القائمة معطلة مع توضيح السبب.</summary>
        private void RefreshProductList()
        {
            _compatibleProducts.Clear();
            foreach (var p in SupplierProductService.GetProductsForSupplier(_selectedSupplier))
                _compatibleProducts.Add(p);

            ProductPickerCombo.SelectedItem = null;
            ProductPickerCombo.IsEnabled = _compatibleProducts.Count > 0;

            if (_selectedSupplier == null)
                ProductFilterHint.Text = LocalizationService.Get("S156");
            else if (_compatibleProducts.Count == 0)
                ProductFilterHint.Text = LocalizationService.Get("S157");
            else
                ProductFilterHint.Text = string.Empty;

            ProductFilterHint.Visibility = string.IsNullOrEmpty(ProductFilterHint.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>عند تغيير الممون: يعرض السطور غير المرتبطة بالممون الجديد ويحذفها بعد موافقة المستخدم. يعيد false إن رفض.</summary>
        private bool ConfirmRemoveIncompatibleLines(Supplier newSupplier)
        {
            var incompatible = _lines
                .Where(l => l.ProductId != Guid.Empty && !SupplierProductService.IsLinked(newSupplier, l.ProductId))
                .ToList();
            if (incompatible.Count == 0) return true;

            string names = string.Join("\n", incompatible.Select(l => "• " + l.ProductName));
            var result = MessageBox.Show(
                $"السلع التالية غير مرتبطة بالممون \"{newSupplier.Name}\":\n\n{names}\n\nهل تريد حذفها من الفاتورة ومتابعة تغيير الممون؟",
                "تأكيد تغيير الممون", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return false;

            foreach (var line in incompatible) _lines.Remove(line);
            return true;
        }

        /// <summary>بعد تغيير الممون: تُحدَّث أسعار الشراء في السطور المتبقية إلى أسعار الممون الجديد.</summary>
        private void RefreshLinePurchasePrices()
        {
            if (_selectedSupplier == null) return;
            foreach (var line in _lines.Where(l => l.ProductId != Guid.Empty))
            {
                var product = DataStore.Products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product != null) line.CostPrice = SupplierProductService.GetPurchasePrice(_selectedSupplier, product);
            }
        }

        private void AddLineFromStock_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSupplier == null)
            {
                MessageBox.Show("الرجاء اختيار ممون أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductPickerCombo.SelectedItem is Product p)
            {
                // حماية إضافية (طبقة منطق العمل) حتى لا يمكن تجاوز تصفية الواجهة
                if (!SupplierProductService.IsLinked(_selectedSupplier, p.Id))
                {
                    MessageBox.Show($"السلعة \"{p.Name}\" غير مرتبطة بالممون \"{_selectedSupplier.Name}\".", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshProductList();
                    return;
                }

                _lines.Add(new InvoiceLine
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Unit = p.Unit,
                    Quantity = 1,
                    UnitPrice = p.UnitPrice,
                    Barcode = p.Barcode,
                    // سعر الشراء الخاص بهذا الممون (SupplierProducts) بدل السعر العام إن وُجد
                    CostPrice = SupplierProductService.GetPurchasePrice(_selectedSupplier, p)
                });
            }
            else
            {
                MessageBox.Show("الرجاء اختيار سلعة من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>عند اختيار "أجل" (بيع بالدين) نقترح تلقائياً أن الفاتورة غير مدفوعة بعد؛ يمكن للمستخدم تغيير ذلك يدوياً.</summary>
        private void PaymentMethodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsPaidCheckBox == null) return;
            string? method = (PaymentMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            bool isCredit = method == LocalizationService.Get("S090"); // "أجل" / "À crédit"
            IsPaidCheckBox.IsChecked = !isCredit;
        }

        private void AddManualLine_Click(object sender, RoutedEventArgs e)
        {
            if (!SupplierProductService.AllowManualLines)
            {
                MessageBox.Show("إضافة سطر يدوي غير مسموحة. اختر سلعة من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _lines.Add(new InvoiceLine
            {
                ProductName = "سلعة جديدة",
                Unit = "قطعة",
                Quantity = 1,
                UnitPrice = 0
            });
        }

        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (LinesGrid.SelectedItem is InvoiceLine line)
            {
                _lines.Remove(line);
            }
            else
            {
                MessageBox.Show("الرجاء اختيار سطر من الجدول أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LinesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // نعيد الحساب بعد انتهاء التعديل مباشرة (بالإضافة لما يحدث عبر PropertyChanged)
            Dispatcher.BeginInvoke(new Action(RecalculateTotals));
        }

        private void Totals_Changed(object sender, TextChangedEventArgs e) => RecalculateTotals();

        private void RecalculateTotals()
        {
            if (SubTotalText == null) return; // أثناء التهيئة الأولى قبل بناء الواجهة بالكامل

            decimal subTotal = _lines.Sum(l => l.Total);

            decimal.TryParse(VatRateBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRate);
            decimal.TryParse(FiscalStampBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var stamp);

            decimal vatAmount = Math.Round(subTotal * vatRate / 100m, 2);
            decimal grandTotal = Math.Round(subTotal + vatAmount + stamp, 2);

            SubTotalText.Text = $"{subTotal:N2} د.ج";
            VatText.Text = $"{vatAmount:N2} د.ج";
            GrandTotalText.Text = $"{grandTotal:N2} د.ج";
        }

        private bool TryBuildInvoice(out Invoice invoice)
        {
            invoice = new Invoice();

            if (string.IsNullOrWhiteSpace(CustomerNameBox.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم الزبون أو اختياره من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (_lines.Count == 0)
            {
                MessageBox.Show("الرجاء إضافة سطر واحد على الأقل إلى الفاتورة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            decimal.TryParse(VatRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRate);
            decimal.TryParse(FiscalStampBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var stamp);

            // الرقم التلقائي يُحسب من جديد لحظة الحفظ (حسب الممون والتاريخ) حتى لا يتكرر؛ والرقم اليدوي يبقى كما أدخله المستخدم
            string typedNumber = InvoiceNumberBox.Text.Trim();
            invoice.Date = InvoiceDatePicker.SelectedDate ?? DateTime.Now;
            invoice.Number = typedNumber.Length == 0 || typedNumber == _lastAutoNumber
                ? DataStore.GetNextInvoiceNumber(SupplierNameBox.Text, invoice.Date)
                : typedNumber;

            invoice.SupplierId = _selectedSupplier?.Id ?? Guid.Empty;
            invoice.SupplierName = SupplierNameBox.Text.Trim();
            invoice.SupplierActivity = SupplierActivityBox.Text.Trim();
            invoice.SupplierAddress = SupplierAddressBox.Text.Trim();
            invoice.SupplierCommercialRegister = BidiHelper.Clean(SupplierCommercialRegisterBox.Text);
            invoice.SupplierTaxId = SupplierTaxIdBox.Text.Trim();
            invoice.SupplierArticleNumber = SupplierArticleNumberBox.Text.Trim();

            invoice.CustomerId = _selectedCustomer?.Id ?? Guid.Empty;
            invoice.CustomerName = CustomerNameBox.Text.Trim();
            invoice.CustomerActivity = CustomerActivityBox.Text.Trim();
            invoice.CustomerAddress = CustomerAddressBox.Text.Trim();
            invoice.CustomerCommercialRegister = BidiHelper.Clean(CustomerCommercialRegisterBox.Text);
            invoice.CustomerTaxId = CustomerTaxIdBox.Text.Trim();
            invoice.VatRate = vatRate;
            invoice.FiscalStamp = stamp;
            invoice.PaymentMethod = (PaymentMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "نقدا";
            invoice.Lines = _lines.ToList();
            invoice.IsPaid = IsPaidCheckBox.IsChecked == true;

            // تحقق نهائي من وجود علاقة SupplierProduct لكل سلعة مع الممون (يُنفَّذ عند الحفظ والمعاينة معاً)
            var compatibilityErrors = SupplierProductService.ValidateInvoice(invoice);
            if (compatibilityErrors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", compatibilityErrors), "سلع غير مرتبطة بالممون", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildInvoice(out var invoice)) return;

            DataStore.Invoices.Add(invoice);
            DataStore.SaveInvoices();
            DataStore.DeductStockForInvoice(invoice);
            MessageBox.Show("تم حفظ الفاتورة بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void PrintPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildInvoice(out var invoice)) return;

            var preview = new InvoicePrintWindow(invoice);
            preview.ShowDialog();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
