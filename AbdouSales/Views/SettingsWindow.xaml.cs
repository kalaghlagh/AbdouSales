using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AbdouSales.Services;

namespace AbdouSales.Views
{
    /// <summary>
    /// الإعدادات: (1) المظهر (فاتح/داكن) واللغة، (2) إظهار/إخفاء الأزرار والخيارات في كل نافذة، (3) معلومات الشركة.
    /// تغييرات المظهر واللغة والإظهار/الإخفاء تُحفظ وتُطبَّق فوراً؛ أما زر «حفظ» فيحفظ معلومات الشركة.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly Brush ActiveLangBrush = (Brush)new BrushConverter().ConvertFromString("#1F4E8C")!;
        private static readonly Brush InactiveLangBrush = (Brush)new BrushConverter().ConvertFromString("#C7CFDA")!;

        private bool _loading = true;   // يمنع تنفيذ أحداث الاختيار أثناء تهيئة النافذة
        private bool _building;         // يمنع الحفظ المتكرر أثناء بناء/تعديل قائمة العناصر

        public SettingsWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة

            var c = DataStore.Company;
            NameBox.Text = c.Name;
            ActivityBox.Text = c.Activity;
            AddressBox.Text = c.Address;
            CommercialRegisterBox.Text = c.CommercialRegister;
            TaxIdBox.Text = c.TaxId;
            ArticleNumberBox.Text = c.ArticleNumber;
            PhoneBox.Text = c.Phone;

            ThemeLightRadio.IsChecked = !ThemeService.IsDark;
            ThemeDarkRadio.IsChecked = ThemeService.IsDark;

            RefreshLanguageButtons();
            BuildVisibilityList();

            _loading = false;
            TabAppearance.IsChecked = true; // يعرض قسم المظهر أولاً
        }

        // ===================== الأقسام =====================

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (AppearancePanel == null || VisibilityPanel == null || CompanyPanel == null) return;
            AppearancePanel.Visibility = TabAppearance.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            VisibilityPanel.Visibility = TabVisibility.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            CompanyPanel.Visibility = TabCompany.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // ===================== المظهر =====================

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            ThemeService.Apply(ThemeDarkRadio.IsChecked == true ? ThemeService.Dark : ThemeService.Light);
        }

        // ===================== اللغة =====================

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
            BuildVisibilityList(); // تحديث أسماء العناصر باللغة الجديدة
        }

        private void FrenchLang_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.SetLanguage(LocalizationService.French);
            RefreshLanguageButtons();
            BuildVisibilityList();
        }

        // ===================== إظهار / إخفاء العناصر =====================

        /// <summary>يبني بطاقة لكل نافذة فيها خانة اختيار لكل زر/خيار (محدد = ظاهر، غير محدد = مخفي).</summary>
        private void BuildVisibilityList()
        {
            _building = true;
            VisibilityHost.Children.Clear();

            foreach (var window in UiCatalog.Windows)
            {
                var card = new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                card.SetResourceReference(Border.BackgroundProperty, "BrushSurfaceAlt");
                card.SetResourceReference(Border.BorderBrushProperty, "BrushBorder");

                var stack = new StackPanel();

                var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                var title = new TextBlock { FontWeight = FontWeights.Bold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
                title.SetResourceReference(TextBlock.TextProperty, window.TitleKey);
                title.SetResourceReference(TextBlock.ForegroundProperty, "BrushHeading");
                header.Children.Add(title);

                var panel = new WrapPanel { Orientation = Orientation.Horizontal };

                header.Children.Add(MakeBulkButton("S195", panel, true));
                header.Children.Add(MakeBulkButton("S196", panel, false));
                stack.Children.Add(header);

                foreach (var element in window.Elements)
                {
                    var box = new CheckBox
                    {
                        Tag = element.Key,
                        FontSize = 13.5,
                        Margin = new Thickness(0, 0, 20, 6),
                        IsChecked = !UiVisibility.IsHidden(element.Key)
                    };
                    if (element.SuffixKey == null)
                        box.SetResourceReference(ContentControl.ContentProperty, element.LabelKey);
                    else
                        box.Content = LocalizationService.Get(element.LabelKey) + " " + LocalizationService.Get(element.SuffixKey);

                    box.Checked += ElementCheck_Changed;
                    box.Unchecked += ElementCheck_Changed;
                    panel.Children.Add(box);
                }

                stack.Children.Add(panel);
                card.Child = stack;
                VisibilityHost.Children.Add(card);
            }

            _building = false;
        }

        private Button MakeBulkButton(string labelKey, WrapPanel panel, bool show)
        {
            var button = new Button
            {
                Style = (Style)FindResource("SecondaryButton"),
                Padding = new Thickness(10, 4, 10, 4),
                FontSize = 12,
                Margin = new Thickness(0, 0, 6, 0)
            };
            button.SetResourceReference(ContentControl.ContentProperty, labelKey);
            button.Click += (_, _) =>
            {
                _building = true;
                foreach (var box in panel.Children.OfType<CheckBox>())
                {
                    box.IsChecked = show;
                    UiVisibility.SetHidden((string)box.Tag, !show);
                }
                _building = false;
                UiVisibility.Save();
            };
            return button;
        }

        private void ElementCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_building || sender is not CheckBox box || box.Tag is not string key) return;
            UiVisibility.SetHidden(key, box.IsChecked != true);
            UiVisibility.Save();
        }

        private void ShowEverything_Click(object sender, RoutedEventArgs e)
        {
            UiVisibility.ShowAll();
            UiVisibility.Save();
            BuildVisibilityList();
        }

        // ===================== معلومات الشركة =====================

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var c = DataStore.Company;
            c.Name = NameBox.Text.Trim();
            c.Activity = ActivityBox.Text.Trim();
            c.Address = AddressBox.Text.Trim();
            c.CommercialRegister = BidiHelper.Clean(CommercialRegisterBox.Text);
            c.TaxId = TaxIdBox.Text.Trim();
            c.ArticleNumber = ArticleNumberBox.Text.Trim();
            c.Phone = PhoneBox.Text.Trim();

            DataStore.SaveCompany();
            MessageBox.Show("تم حفظ معلومات الشركة بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void ChooseTemplate_Click(object sender, RoutedEventArgs e)
        {
            var chooser = new TemplateChooserWindow(DataStore.Company.DefaultTemplate, defaultToSaveAsDefault: true) { Owner = this };
            if (chooser.ShowDialog() == true)
            {
                DataStore.Company.DefaultTemplate = chooser.SelectedStyle;
                DataStore.SaveCompany();
                MessageBox.Show(LocalizationService.Get("S151"), LocalizationService.Get("S152"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
