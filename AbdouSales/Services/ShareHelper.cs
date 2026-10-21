using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace AbdouSales.Services
{
    /// <summary>
    /// أدوات مشاركة ملف الفاتورة (PDF) عبر واتساب أو جيميل.
    /// ملاحظة مهمة: لا يسمح واتساب ولا جيميل لأي برنامج خارجي بإرفاق ملف تلقائياً
    /// (هذا قيد أمني من الشركتين وليس نقصاً في البرنامج). لذلك يقوم البرنامج بـ:
    ///   1) إنشاء ملف PDF للفاتورة،
    ///   2) نسخ الملف إلى الحافظة،
    ///   3) فتح واتساب أو جيميل مع نص الرسالة جاهزاً،
    /// ويبقى على المستخدم الضغط على Ctrl+V داخل المحادثة أو الرسالة ليُرفق الملف فوراً.
    /// </summary>
    public static class ShareHelper
    {
        /// <summary>ينسخ ملف الفاتورة إلى الحافظة حتى يمكن لصقه بـ Ctrl+V في واتساب أو جيميل.</summary>
        public static bool CopyFileToClipboard(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            var files = new StringCollection { filePath };

            // الحافظة قد تكون مشغولة من طرف برنامج آخر، لذلك نعيد المحاولة عدة مرات.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.SetFileDropList(files);
                    return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(120);
                }
            }
            return false;
        }

        /// <summary>يفتح محادثة واتساب (تطبيق سطح المكتب إن وُجد، وإلا واتساب ويب) مع نص جاهز.</summary>
        public static void OpenWhatsApp(string message, string? phoneNumber = null)
        {
            string text = Uri.EscapeDataString(message ?? string.Empty);
            string? phone = NormalizePhone(phoneNumber);

            string desktopUrl = string.IsNullOrEmpty(phone)
                ? $"whatsapp://send?text={text}"
                : $"whatsapp://send?phone={phone}&text={text}";

            string webUrl = string.IsNullOrEmpty(phone)
                ? $"https://web.whatsapp.com/send?text={text}"
                : $"https://web.whatsapp.com/send?phone={phone}&text={text}";

            // نحاول أولاً تطبيق واتساب المثبت على الجهاز، وإن لم يكن مثبتاً نفتح واتساب ويب.
            if (!TryOpen(desktopUrl))
            {
                TryOpen(webUrl);
            }
        }

        /// <summary>يفتح نافذة إنشاء رسالة جديدة في جيميل مع الموضوع والنص جاهزين.</summary>
        public static void OpenGmail(string subject, string body, string? toEmail = null)
        {
            var url = new StringBuilder("https://mail.google.com/mail/?view=cm&fs=1");
            if (!string.IsNullOrWhiteSpace(toEmail))
                url.Append("&to=").Append(Uri.EscapeDataString(toEmail.Trim()));
            url.Append("&su=").Append(Uri.EscapeDataString(subject ?? string.Empty));
            url.Append("&body=").Append(Uri.EscapeDataString(body ?? string.Empty));

            TryOpen(url.ToString());
        }

        /// <summary>يفتح مستكشف الملفات على الفاتورة المحددة.</summary>
        public static void ShowFileInExplorer(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    string? folder = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
                }
            }
            catch
            {
                // نتجاهل أي خطأ حتى لا يتعطل البرنامج
            }
        }

        /// <summary>يفتح ملف PDF ببرنامج القراءة الافتراضي على الجهاز.</summary>
        public static void OpenFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private static bool TryOpen(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// يحوّل رقم الهاتف إلى الصيغة الدولية التي يطلبها واتساب (أرقام فقط بدون + ولا مسافات).
        /// مثال جزائري: 0555123456 يصبح 213555123456
        /// </summary>
        public static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            var digits = new StringBuilder();
            foreach (char c in phone)
            {
                if (char.IsDigit(c)) digits.Append(c);
            }

            string value = digits.ToString();
            if (value.Length == 0) return null;

            // رقم محلي جزائري يبدأ بـ 0 -> نضيف رمز الجزائر 213
            if (value.StartsWith("00")) value = value.Substring(2);
            else if (value.StartsWith("0")) value = "213" + value.Substring(1);

            return value.Length >= 8 ? value : null;
        }
    }
}
