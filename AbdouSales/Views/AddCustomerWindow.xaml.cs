using System.Windows;
using System.Windows.Controls;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class AddCustomerWindow : Window
    {
        private Customer? _selected;

        public AddCustomerWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            CustomersGrid.ItemsSource = DataStore.Customers;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم الزبون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selected != null)
            {
                // تعديل زبون موجود
                _selected.Name = NameBox.Text.Trim();
                _selected.Activity = ActivityBox.Text.Trim();
                _selected.Address = AddressBox.Text.Trim();
                _selected.Phone = PhoneBox.Text.Trim();
                _selected.CommercialRegister = BidiHelper.Clean(CommercialRegisterBox.Text);
                _selected.TaxId = TaxIdBox.Text.Trim();
            }
            else
            {
                DataStore.Customers.Add(new Customer
                {
                    Name = NameBox.Text.Trim(),
                    Activity = ActivityBox.Text.Trim(),
                    Address = AddressBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim(),
                    CommercialRegister = BidiHelper.Clean(CommercialRegisterBox.Text),
                    TaxId = TaxIdBox.Text.Trim()
                });
            }

            DataStore.SaveCustomers();
            CustomersGrid.Items.Refresh();
            Clear_Click(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (CustomersGrid.SelectedItem is Customer c)
            {
                var result = MessageBox.Show($"هل تريد حذف الزبون \"{c.Name}\"؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataStore.Customers.Remove(c);
                    DataStore.SaveCustomers();
                    Clear_Click(sender, e);
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار زبون من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            CustomersGrid.SelectedItem = null;
        }

        private void CustomersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CustomersGrid.SelectedItem is Customer c)
            {
                _selected = c;
                NameBox.Text = c.Name;
                ActivityBox.Text = c.Activity;
                AddressBox.Text = c.Address;
                PhoneBox.Text = c.Phone;
                CommercialRegisterBox.Text = c.CommercialRegister;
                TaxIdBox.Text = c.TaxId;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelImportUi.RunImport(this, ExcelImportUi.Kind.Customers);
        }

        private void ExcelTemplate_Click(object sender, RoutedEventArgs e)
            => ExcelImportUi.SaveTemplate(this, ExcelImportUi.Kind.Customers);

        private void CommercialRegisterBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
