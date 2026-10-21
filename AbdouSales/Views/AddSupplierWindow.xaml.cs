using System.Windows;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class AddSupplierWindow : Window
    {
        private Supplier? _selected;

        public AddSupplierWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            SuppliersGrid.ItemsSource = DataStore.Suppliers;
            ActivityBox.ItemsSource = DataStore.Activities; // الأنشطة الموجودة؛ ويمكن كتابة نشاط جديد مباشرة فيُنشأ عند الحفظ
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم الممون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // النشاط اختياري الآن (وصف فقط)؛ التصفية في الفواتير تعتمد على علاقة الممون بالسلع (SupplierProducts)
            var activity = string.IsNullOrWhiteSpace(ActivityBox.Text) ? null : DataStore.GetOrCreateActivity(ActivityBox.Text);

            if (_selected != null)
            {
                _selected.Name = NameBox.Text.Trim();
                _selected.Activity = activity?.Name ?? string.Empty;
                _selected.ActivityId = activity?.Id;
                _selected.Address = AddressBox.Text.Trim();
                _selected.Phone = PhoneBox.Text.Trim();
                _selected.CommercialRegister = BidiHelper.Clean(CommercialRegisterBox.Text);
                _selected.TaxId = TaxIdBox.Text.Trim();
                _selected.ArticleNumber = ArticleNumberBox.Text.Trim();
            }
            else
            {
                DataStore.Suppliers.Add(new Supplier
                {
                    Name = NameBox.Text.Trim(),
                    Activity = activity?.Name ?? string.Empty,
                    ActivityId = activity?.Id,
                    Address = AddressBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim(),
                    CommercialRegister = BidiHelper.Clean(CommercialRegisterBox.Text),
                    TaxId = TaxIdBox.Text.Trim(),
                    ArticleNumber = ArticleNumberBox.Text.Trim()
                });
            }

            DataStore.SaveSuppliers();
            SuppliersGrid.Items.Refresh();
            Clear_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SuppliersGrid.SelectedItem is Supplier s)
            {
                var result = MessageBox.Show($"هل تريد حذف الممون \"{s.Name}\"؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataStore.Suppliers.Remove(s);
                    DataStore.SaveSuppliers();
                    DataStore.RemoveLinksForSupplier(s.Id);
                    Clear_Click(sender, e);
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار ممون من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            NameBox.Text = "";
            ActivityBox.Text = "";
            AddressBox.Text = "";
            PhoneBox.Text = "";
            CommercialRegisterBox.Text = "";
            TaxIdBox.Text = "";
            ArticleNumberBox.Text = "";
            SuppliersGrid.SelectedItem = null;
        }

        private void SuppliersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SuppliersGrid.SelectedItem is Supplier s)
            {
                _selected = s;
                NameBox.Text = s.Name;
                ActivityBox.Text = s.Activity;
                AddressBox.Text = s.Address;
                PhoneBox.Text = s.Phone;
                CommercialRegisterBox.Text = s.CommercialRegister;
                TaxIdBox.Text = s.TaxId;
                ArticleNumberBox.Text = s.ArticleNumber;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelImportUi.RunImport(this, ExcelImportUi.Kind.Suppliers);
        }

        private void ExcelTemplate_Click(object sender, RoutedEventArgs e)
            => ExcelImportUi.SaveTemplate(this, ExcelImportUi.Kind.Suppliers);
    }
}
