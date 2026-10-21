using System.Windows;
using System.Windows.Media;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    public partial class MainWindow : Window
    {
        private static readonly Brush ActiveLangBrush = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1F4E8C")!;
        private static readonly Brush InactiveLangBrush = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#C7CFDA")!;

        public MainWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UiVisibility.Apply(this); // إخفاء/إظهار الأزرار والخيارات حسب إعدادات المستخدم
            RefreshLanguageButtons();
        }

        private void RefreshLanguageButtons()
        {
            bool isFrench = LocalizationService.CurrentLanguage == LocalizationService.French;
            ArabicLangButton.Background = isFrench ? InactiveLangBrush : ActiveLangBrush;
            ArabicLangButton.Foreground = isFrench ? Brushes.Black : Brushes.White;
            FrenchLangButton.Background = isFrench ? ActiveLangBrush : InactiveLangBrush;
            FrenchLangButton.Foreground = isFrench ? Brushes.White : Brushes.Black;
        }

        private void ArabicLang_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.SetLanguage(LocalizationService.Arabic);
            RefreshLanguageButtons();
        }

        private void FrenchLang_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.SetLanguage(LocalizationService.French);
            RefreshLanguageButtons();
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            new NewInvoiceWindow().ShowDialog();
        }

        private void Cashier_Click(object sender, RoutedEventArgs e)
        {
            new CashierWindow().ShowDialog();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            new DashboardWindow().ShowDialog();
        }

        private void TemplateChooser_Click(object sender, RoutedEventArgs e)
        {
            var chooser = new TemplateChooserWindow(DataStore.Company.DefaultTemplate, defaultToSaveAsDefault: true)
            {
                Owner = this
            };
            if (chooser.ShowDialog() == true)
            {
                DataStore.Company.DefaultTemplate = chooser.SelectedStyle;
                DataStore.SaveCompany();
                MessageBox.Show(LocalizationService.Get("S151"), LocalizationService.Get("S152"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            new AddCustomerWindow().ShowDialog();
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            new AddSupplierWindow().ShowDialog();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            new AddProductWindow().ShowDialog();
        }

        private void InvoiceList_Click(object sender, RoutedEventArgs e)
        {
            new InvoiceListWindow().ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
            UiVisibility.Apply(this); // قد يكون المستخدم غيّر العناصر المخفية في الإعدادات
        }
    }
}
