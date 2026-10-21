using System.Windows;
using AbdouSales.Services;

namespace AbdouSales
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // تحميل جميع البيانات المحفوظة (الزبائن، الممونين، السلع، الفواتير، معلومات الشركة)
            DataStore.Load();

            // تطبيق لغة الواجهة المحفوظة (عربي افتراضياً) قبل فتح أي نافذة
            LocalizationService.Initialize(DataStore.Company.Language);

            // تطبيق مظهر الواجهة المحفوظ (فاتح افتراضياً) قبل فتح أي نافذة
            ThemeService.Initialize(DataStore.Company.Theme);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // حفظ احتياطي عند الإغلاق (كل شاشة تحفظ فور التعديل، وهذا فقط للاحتياط)
            DataStore.SaveAll();
            base.OnExit(e);
        }
    }
}
