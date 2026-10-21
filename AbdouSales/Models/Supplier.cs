using System;

namespace AbdouSales.Models
{
    public class Supplier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;      // اسم الممون
        public string Activity { get; set; } = string.Empty;  // النشاط (اسم النشاط، نسخة نصية تُستعمل في ترويسة الفاتورة)

        /// <summary>النشاط التجاري المرتبط بالممون (مفتاح أجنبي نحو BusinessActivity). يحدد السلع المسموح ببيعها في فواتيره.</summary>
        public Guid? ActivityId { get; set; }
        public string Address { get; set; } = string.Empty;   // العنوان
        public string Phone { get; set; } = string.Empty;      // الهاتف

        public string TaxId { get; set; } = string.Empty;               // الرقم الجبائي (TIN)
        public string ArticleNumber { get; set; } = string.Empty;       // رقم المادة
        public string CommercialRegister { get; set; } = string.Empty;  // رقم السجل التجاري (CRN)

        public override string ToString() => Name;
    }
}
