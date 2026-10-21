using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class AddProductWindow : Window
    {
        private Product? _selected;
        private static readonly Random _rng = new();

        /// <summary>سطر في جدول ممونيي السلعة: هل الممون محدد + سعر الشراء لديه + رمز السلعة عنده.</summary>
        public sealed class SupplierChoice : INotifyPropertyChanged
        {
            private bool _isSelected;
            public Guid SupplierId { get; init; }
            public string Name { get; init; } = string.Empty;
            public string PurchasePriceText { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;

            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public AddProductWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            ProductsGrid.ItemsSource = DataStore.Products;
            ClearSelection(); // التحديد حالة واجهة فقط: نبدأ دائماً بدون سلع محددة
            LoadSupplierChoices(null);
            Closed += (_, _) => ClearSelection();
        }

        // ===================== تحديد عدة سلع (حذف / إضافة لممون آخر) =====================

        private static void ClearSelection()
        {
            foreach (var p in DataStore.Products) p.IsSelected = false;
        }

        private System.Collections.Generic.List<Product> GetCheckedProducts()
            => DataStore.Products.Where(p => p.IsSelected).ToList();

        private void UpdateSelectedCount()
            => SelectedCountText.Text = DataStore.Products.Count(p => p.IsSelected).ToString(CultureInfo.InvariantCulture);

        private void SelectCheck_Click(object sender, RoutedEventArgs e) => UpdateSelectedCount();

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in DataStore.Products) p.IsSelected = true;
            UpdateSelectedCount();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            UpdateSelectedCount();
        }

        /// <summary>يربط السلع المحددة بممون آخر (إضافة)، أو ينقلها إليه ويزيل ارتباطها بالممونين الحاليين (نقل).</summary>
        private void AssignSupplier_Click(object sender, RoutedEventArgs e)
        {
            var products = GetCheckedProducts();
            if (products.Count == 0)
            {
                MessageBox.Show("حدّد سلعة واحدة على الأقل من القائمة أولاً (خانة التحديد).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DataStore.Suppliers.Count == 0)
            {
                MessageBox.Show("لا يوجد أي ممون مسجل. أضف ممونًا أولاً من شاشة إضافة ممون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new AssignSupplierWindow(products.Count) { Owner = this };
            if (dlg.ShowDialog() != true || dlg.ChosenSupplier == null) return;

            var target = dlg.ChosenSupplier;
            int added = 0, alreadyLinked = 0;

            foreach (var product in products)
            {
                var existing = DataStore.GetLinksForProduct(product.Id);
                bool targetHadLink = existing.Any(l => l.SupplierId == target.Id);

                if (targetHadLink) alreadyLinked++;
                else
                {
                    // عند النقل نحمل سعر الشراء ورمز السلعة من الممون السابق ليُعدَّلا لاحقاً عند الحاجة
                    var source = dlg.MoveProducts ? existing.FirstOrDefault() : null;
                    DataStore.UpsertSupplierProduct(target.Id, product.Id, source?.PurchasePrice, source?.SupplierProductCode);
                    added++;
                }

                if (dlg.MoveProducts)
                {
                    foreach (var old in existing.Where(l => l.SupplierId != target.Id).ToList())
                        DataStore.SupplierProducts.Remove(old);
                }
            }
            DataStore.SaveSupplierProducts();

            ClearSelection();
            UpdateSelectedCount();
            LoadSupplierChoices(_selected); // تحديث جدول ممونيي السلعة المعروضة حاليًا

            string verb = dlg.MoveProducts ? "نقل" : "إضافة";
            string msg = $"تمت عملية ال{verb} إلى الممون \"{target.Name}\": {added} سلعة.";
            if (alreadyLinked > 0) msg += $"\n{alreadyLinked} سلعة كانت مرتبطة به من قبل.";
            MessageBox.Show(msg, "تم", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>يبني جدول الممونين؛ إن أُعطيت سلعة تُحدَّد ممونوها الحاليون مع أسعارهم ورموزهم.</summary>
        private void LoadSupplierChoices(Product? product)
        {
            var links = product == null
                ? new System.Collections.Generic.Dictionary<Guid, SupplierProduct>()
                : DataStore.GetLinksForProduct(product.Id).ToDictionary(l => l.SupplierId);

            SuppliersLinkGrid.ItemsSource = DataStore.Suppliers
                .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(s =>
                {
                    links.TryGetValue(s.Id, out var link);
                    return new SupplierChoice
                    {
                        SupplierId = s.Id,
                        Name = s.Name,
                        IsSelected = link != null,
                        PurchasePriceText = link?.PurchasePrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                        Code = link?.SupplierProductCode ?? string.Empty
                    };
                })
                .ToList();
        }

        /// <summary>
        /// يقرأ الممونين المحددين مع التحقق من سعر الشراء (فارغ = بلا سعر خاص، وغير الصالح أو السالب = خطأ).
        /// يعيد null بعد عرض رسالة الخطأ.
        /// </summary>
        private System.Collections.Generic.List<(Guid SupplierId, decimal? PurchasePrice, string Code)>? ReadSelectedSuppliers()
        {
            SuppliersLinkGrid.CommitEdit(DataGridEditingUnit.Row, true); // تثبيت الخلية قيد التحرير قبل القراءة

            var result = new System.Collections.Generic.List<(Guid, decimal?, string)>();
            foreach (var row in SuppliersLinkGrid.Items.OfType<SupplierChoice>().Where(r => r.IsSelected))
            {
                decimal? price = null;
                string text = (row.PurchasePriceText ?? string.Empty).Trim();
                if (text.Length > 0)
                {
                    if (!decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                    {
                        MessageBox.Show($"سعر الشراء غير صالح للممون \"{row.Name}\".", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return null;
                    }
                    price = parsed;
                }
                result.Add((row.SupplierId, price, (row.Code ?? string.Empty).Trim()));
            }
            return result;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("الرجاء إدخال تعيين السلعة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                MessageBox.Show("الرجاء إدخال سعر صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal.TryParse(StockBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var stock);
            decimal.TryParse(CostPriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var costPrice);

            string unitText = (UnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? UnitBox.Text;
            if (string.IsNullOrWhiteSpace(unitText)) unitText = "قطعة";

            string barcode = BarcodeBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var duplicate = DataStore.FindProductByBarcode(barcode);
                if (duplicate != null && duplicate != _selected)
                {
                    if (_selected != null)
                    {
                        MessageBox.Show($"هذا الرمز الشريطي مستعمل بالفعل من طرف السلعة \"{duplicate.Name}\".", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // السلعة موجودة: نعرض ربطها بالممونين المحددين بدل إنشاء سلعة مكررة
                    var answer = MessageBox.Show(
                        $"السلعة \"{duplicate.Name}\" موجودة بالفعل بهذا الرمز الشريطي.\n\nهل تريد ربطها بالممونين المحددين بدل إنشاء سلعة مكررة؟",
                        "سلعة موجودة", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer != MessageBoxResult.Yes) return;

                    var extra = ReadSelectedSuppliers();
                    if (extra == null) return;
                    if (extra.Count == 0)
                    {
                        MessageBox.Show("حدّد ممونًا واحدًا على الأقل لربطه بهذه السلعة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // إضافة/تحديث الروابط فقط دون حذف روابط الممونين الآخرين ودون تعديل بيانات السلعة
                    foreach (var (supplierId, purchasePrice, code) in extra)
                        DataStore.UpsertSupplierProduct(supplierId, duplicate.Id, purchasePrice, code);
                    DataStore.SaveSupplierProducts();
                    Clear_Click(sender, e);
                    return;
                }
            }

            var selectedSuppliers = ReadSelectedSuppliers();
            if (selectedSuppliers == null) return;

            Product saved;
            if (_selected != null)
            {
                saved = _selected;
                _selected.Name = NameBox.Text.Trim();
                _selected.Unit = unitText;
                _selected.UnitPrice = price;
                _selected.CostPrice = costPrice;
                _selected.QuantityInStock = stock;
                _selected.Barcode = barcode;
            }
            else
            {
                saved = new Product
                {
                    Name = NameBox.Text.Trim(),
                    Unit = unitText,
                    UnitPrice = price,
                    CostPrice = costPrice,
                    QuantityInStock = stock,
                    Barcode = barcode
                };
                DataStore.Products.Add(saved);
            }

            DataStore.SaveProducts();
            DataStore.SetProductSuppliers(saved.Id, selectedSuppliers);
            ProductsGrid.Items.Refresh();
            Clear_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            // أولوية للسلع المحددة بخانة التحديد؛ وإلا نحذف السلعة المظللة في الجدول (السلوك السابق)
            var checkedProducts = GetCheckedProducts();
            if (checkedProducts.Count > 0)
            {
                string question = checkedProducts.Count == 1
                    ? $"هل تريد حذف السلعة \"{checkedProducts[0].Name}\"؟"
                    : $"هل تريد حذف {checkedProducts.Count} سلعة محددة؟ لا يمكن التراجع عن هذا الإجراء.";
                if (MessageBox.Show(question, "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                foreach (var p in checkedProducts)
                {
                    DataStore.Products.Remove(p);
                    DataStore.RemoveLinksForProduct(p.Id);
                }
                DataStore.SaveProducts();
                Clear_Click(sender, e);
                UpdateSelectedCount();
                return;
            }

            if (ProductsGrid.SelectedItem is Product single)
            {
                var result = MessageBox.Show($"هل تريد حذف السلعة \"{single.Name}\"؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataStore.Products.Remove(single);
                    DataStore.SaveProducts();
                    DataStore.RemoveLinksForProduct(single.Id);
                    Clear_Click(sender, e);
                }
            }
            else
            {
                MessageBox.Show("حدّد سلعة أو أكثر من القائمة (خانة التحديد) أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            NameBox.Text = "";
            UnitBox.Text = "";
            UnitBox.SelectedItem = null;
            PriceBox.Text = "";
            CostPriceBox.Text = "";
            StockBox.Text = "";
            BarcodeBox.Text = "";
            LoadSupplierChoices(null);
            ProductsGrid.SelectedItem = null;
        }

        private void ProductsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject src && FindParent<CheckBox>(src) != null) return; // نقر مزدوج على خانة التحديد لا يفتح التعديل
            if (ProductsGrid.SelectedItem is Product p)
            {
                _selected = p;
                NameBox.Text = p.Name;
                UnitBox.Text = p.Unit;
                PriceBox.Text = p.UnitPrice.ToString(CultureInfo.InvariantCulture);
                CostPriceBox.Text = p.CostPrice.ToString(CultureInfo.InvariantCulture);
                StockBox.Text = p.QuantityInStock.ToString(CultureInfo.InvariantCulture);
                BarcodeBox.Text = p.Barcode;
                LoadSupplierChoices(p);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>يولّد رمزاً شريطياً عشوائياً (13 رقماً على غرار EAN-13) للسلع التي لا تملك رمزاً مطبوعاً من المصنع.</summary>
        private void GenerateBarcode_Click(object sender, RoutedEventArgs e)
        {
            string code;
            do
            {
                code = "2" + _rng.Next(100000000, 999999999).ToString(CultureInfo.InvariantCulture) + _rng.Next(100, 999).ToString(CultureInfo.InvariantCulture);
                code = code.Substring(0, 12);
                code += ComputeEan13CheckDigit(code);
            } while (DataStore.FindProductByBarcode(code) != null);

            BarcodeBox.Text = code;
        }

        private static char ComputeEan13CheckDigit(string twelveDigits)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = twelveDigits[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int check = (10 - (sum % 10)) % 10;
            return (char)('0' + check);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelImportUi.RunImport(this, ExcelImportUi.Kind.Products);
            LoadSupplierChoices(_selected); // تحديث الروابط المعروضة بعد الاستيراد
        }

        private void ExcelTemplate_Click(object sender, RoutedEventArgs e)
            => ExcelImportUi.SaveTemplate(this, ExcelImportUi.Kind.Products);
    }
}
