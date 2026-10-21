using System.Collections.Generic;

namespace AbdouSales.Models
{
    public class CompanyInfo
    {
        public string Name { get; set; } = string.Empty;                 // اسم الشركة / المسير
        public string Activity { get; set; } = string.Empty;             // النشاط
        public string Address { get; set; } = string.Empty;              // العنوان
        public string CommercialRegister { get; set; } = string.Empty;   // رقم السجل التجاري
        public string TaxId { get; set; } = string.Empty;                // الرقم الجبائي
        public string ArticleNumber { get; set; } = string.Empty;        // رقم المادة
        public string Phone { get; set; } = string.Empty;                // الهاتف

        /// <summary>القالب الافتراضي المستعمل عند طباعة/تصدير أي فاتورة جديدة.</summary>
        public InvoiceTemplateStyle DefaultTemplate { get; set; } = InvoiceTemplateStyle.ExecutiveNavy;

        /// <summary>لغة واجهة البرنامج: "ar" (العربية) أو "fr" (الفرنسية).</summary>
        public string Language { get; set; } = "ar";

        /// <summary>مظهر البرنامج: "Light" (فاتح) أو "Dark" (داكن).</summary>
        public string Theme { get; set; } = "Light";

        /// <summary>مفاتيح الأزرار والخيارات المخفية في نوافذ البرنامج (تُدار من شاشة الإعدادات).</summary>
        public List<string> HiddenElements { get; set; } = new();
    }
}
