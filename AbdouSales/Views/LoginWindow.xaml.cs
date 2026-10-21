using AbdouSales.Services;
using System.Windows;
using System.Windows.Input;

namespace AbdouSales.Views
{
    public partial class LoginWindow : Window
    {
        // بيانات الدخول (اسم المستخدم / كلمة المرور)
        private const string ValidUsername = "admin";
        private const string ValidPassword = "tam12123";

        public LoginWindow()
        {
            InitializeComponent();
            ScreenFit.Apply(this); // ملاءمة حجم النافذة مع شاشات اللابتوب 14 بوصة
            UsernameBox.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
            }
        }

        private void TryLogin()
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (username == ValidUsername && password == ValidPassword)
            {
                ErrorText.Visibility = Visibility.Collapsed;

                var main = new MainWindow();
                Application.Current.MainWindow = main;
                main.Show();
                Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }
    }
}
