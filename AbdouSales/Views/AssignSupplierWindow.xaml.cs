using System;
using System.Linq;
using System.Windows;
using AbdouSales.Models;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    /// <summary>نافذة صغيرة لاختيار الممون الذي ستُضاف إليه السلع المحددة (مع خيار النقل بدل الإضافة).</summary>
    public partial class AssignSupplierWindow : Window
    {
        public Supplier? ChosenSupplier { get; private set; }
        public bool MoveProducts { get; private set; }

        public AssignSupplierWindow(int productCount)
        {
            InitializeComponent();
            ScreenFit.Apply(this);
            InfoText.Text = $"عدد السلع المحددة: {productCount}";
            SupplierCombo.ItemsSource = DataStore.Suppliers
                .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierCombo.SelectedItem is not Supplier supplier)
            {
                MessageBox.Show("الرجاء اختيار ممون من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ChosenSupplier = supplier;
            MoveProducts = MoveCheck.IsChecked == true;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
