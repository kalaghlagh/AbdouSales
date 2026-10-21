using System;
using System.Collections.Generic;
using System.Linq;

namespace AbdouSales.Models
{
    public class Invoice
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Number { get; set; } = string.Empty;   // رقم الفاتورة
        public DateTime Date { get; set; } = DateTime.Now;    // التاريخ

        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;    // اسم الممون (يظهر في ترويسة الفاتورة بدل بيانات الشركة الافتراضية إن اختير)
        public string SupplierActivity { get; set; } = string.Empty; // النشاط
        public string SupplierAddress { get; set; } = string.Empty;  // العنوان
        public string SupplierCommercialRegister { get; set; } = string.Empty; // رقم السجل التجاري
        public string SupplierTaxId { get; set; } = string.Empty;               // الرقم الجبائي
        public string SupplierArticleNumber { get; set; } = string.Empty;       // رقم المادة

        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;    // اسم الزبون
        public string CustomerActivity { get; set; } = string.Empty; // النشاط
        public string CustomerAddress { get; set; } = string.Empty;  // العنوان
        public string CustomerCommercialRegister { get; set; } = string.Empty; // رقم السجل التجاري
        public string CustomerTaxId { get; set; } = string.Empty;               // الرقم الجبائي

        public List<InvoiceLine> Lines { get; set; } = new();

        public decimal VatRate { get; set; } = 19m;      // نسبة القيمة المضافة %
        public decimal FiscalStamp { get; set; } = 0m;    // الطابع الجبائي
        public string PaymentMethod { get; set; } = "نقدا"; // كيفية التسديد

        /// <summary>القالب المستعمل لهذه الفاتورة تحديداً. إن كان فارغاً (null) يُستعمل قالب الشركة الافتراضي.</summary>
        public InvoiceTemplateStyle? TemplateStyle { get; set; }

        /// <summary>هل أُنشئت هذه الفاتورة من شاشة نقطة البيع (الكاشير)؟</summary>
        public bool IsCashierSale { get; set; } = false;

        /// <summary>هل تم استلام كامل المبلغ؟ يُستعمل لتتبع الفواتير غير المدفوعة (خاصة فواتير "أجل").</summary>
        public bool IsPaid { get; set; } = true;

        public decimal AmountReceived { get; set; } = 0m;   // المبلغ المستلم من الزبون (كاشير)
        public decimal ChangeGiven { get; set; } = 0m;       // الباقي المُرجع للزبون (كاشير)

        public decimal SubTotal => Math.Round(Lines.Sum(l => l.Total), 2);      // المجموع بدون رسوم
        public decimal VatAmount => Math.Round(SubTotal * VatRate / 100m, 2);   // قيمة الرسم
        public decimal GrandTotal => Math.Round(SubTotal + VatAmount + FiscalStamp, 2); // المجموع الكلي

        /// <summary>مجموع أرباح كل سطور الفاتورة (سعر البيع - سعر التكلفة) × الكمية.</summary>
        public decimal Profit => Math.Round(Lines.Sum(l => l.Profit), 2);
    }
}
