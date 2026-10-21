using System;

namespace AbdouSales.Models
{
    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;      // اسم الزبون
        public string Activity { get; set; } = string.Empty;  // النشاط
        public string Address { get; set; } = string.Empty;   // العنوان
        public string Phone { get; set; } = string.Empty;      // الهاتف
        public string CommercialRegister { get; set; } = string.Empty; // رقم السجل التجاري
        public string TaxId { get; set; } = string.Empty;              // الرقم الجبائي

        public override string ToString() => Name;
    }
}
