using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace AbdouSales.Services
{
    /// <summary>الجزء الظاهر للمستخدم من الاستيراد: اختيار الملف، رسالة النتيجة، وحفظ نموذج Excel جاهز.</summary>
    public static class ExcelImportUi
    {
        public enum Kind { Suppliers, Customers, Products }

        private const string Filter = "ملفات Excel (*.xlsx)|*.xlsx";

        public static void RunImport(Window owner, Kind kind)
        {
            var dialog = new OpenFileDialog
            {
                Title = "اختيار ملف Excel للاستيراد",
                Filter = Filter,
                CheckFileExists = true
            };
            if (dialog.ShowDialog(owner) != true) return;

            ImportResult result;
            var previousCursor = Mouse.OverrideCursor;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                result = kind switch
                {
                    Kind.Suppliers => ExcelImportService.ImportSuppliers(dialog.FileName),
                    Kind.Customers => ExcelImportService.ImportCustomers(dialog.FileName),
                    _ => ExcelImportService.ImportProducts(dialog.FileName)
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ غير متوقع أثناء الاستيراد:\n" + ex.Message, "استيراد Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally { Mouse.OverrideCursor = previousCursor; }

            MessageBox.Show(result.ToMessage(), "نتيجة الاستيراد", MessageBoxButton.OK,
                result.FatalError != null ? MessageBoxImage.Error : result.Failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        public static void SaveTemplate(Window owner, Kind kind)
        {
            string[] headers;
            string[] example;
            string fileName;
            switch (kind)
            {
                case Kind.Suppliers:
                    fileName = "نموذج_الممونين.xlsx";
                    headers = new[] { "الاسم", "النشاط", "العنوان", "الهاتف", "السجل التجاري", "الرقم الجبائي", "رقم المادة" };
                    example = new[] { "مثال: شركة النور", "مواد البناء", "الجزائر العاصمة", "0550123456", "21/أ/1236438-00/11", "000123456789012", "12345678901" };
                    break;
                case Kind.Customers:
                    fileName = "نموذج_الزبائن.xlsx";
                    headers = new[] { "الاسم", "النشاط", "العنوان", "الهاتف", "السجل التجاري", "الرقم الجبائي" };
                    example = new[] { "مثال: مؤسسة الأمل", "مقاولات", "وهران", "0660123456", "21/أ/1236438-00/11", "000123456789012" };
                    break;
                default:
                    fileName = "نموذج_السلع.xlsx";
                    headers = new[] { "التعيين", "الوحدة", "سعر البيع", "سعر التكلفة", "الكمية", "الباركود", "السجل التجاري للممون", "سعر الشراء", "رمز السلعة عند الممون" };
                    example = new[] { "مثال: إسمنت", "كيس", "850", "700", "100", "0123456789012", "21/A/123456-00/11", "690", "CIM-50" };
                    break;
            }

            var dialog = new SaveFileDialog
            {
                Title = "حفظ نموذج Excel",
                Filter = Filter,
                FileName = fileName,
                DefaultExt = ".xlsx",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(owner) != true) return;

            try
            {
                ExcelHelper.WriteSimpleWorkbook(dialog.FileName, headers, new[] { example });
                MessageBox.Show(
                    "تم حفظ النموذج. السطر الثاني مجرد مثال: احذفه أو عدّله قبل الاستيراد.\n" +
                    (kind == Kind.Products ? "ربط السلعة بالممون يتم عبر السجل التجاري للممون (أو اسمه)؛ كرر السلعة في سطر آخر لربطها بممون آخر.\nاكتب الباركود كنص (حتى لا يحذف Excel الأصفار في بدايته).\n" : string.Empty) +
                    "اكتب الأرقام الطويلة (الهاتف، الرقم الجبائي) كنص حتى لا يحذف Excel الأصفار في بدايتها.",
                    "نموذج Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر حفظ النموذج:\n" + ex.Message, "نموذج Excel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
