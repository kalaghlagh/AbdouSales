using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AbdouSales.Models
{
    public class InvoiceLine : INotifyPropertyChanged
    {
        private string _productName = string.Empty;
        private string _unit = string.Empty;
        private decimal _quantity;
        private decimal _unitPrice;
        private decimal _costPrice;

        public Guid ProductId { get; set; }

        /// <summary>الرمز الشريطي للسلعة وقت إضافتها للفاتورة (نص، يُحفظ كما هو دون تحويل رقمي).</summary>
        public string Barcode { get; set; } = string.Empty;

        public string ProductName // التعيين
        {
            get => _productName;
            set { _productName = value; OnChanged(); }
        }

        public string Unit // الوحدة
        {
            get => _unit;
            set { _unit = value; OnChanged(); }
        }

        public decimal Quantity // الكمية
        {
            get => _quantity;
            set { _quantity = value; OnChanged(); OnChanged(nameof(Total)); OnChanged(nameof(Profit)); }
        }

        public decimal UnitPrice // سعر الوحدة
        {
            get => _unitPrice;
            set { _unitPrice = value; OnChanged(); OnChanged(nameof(Total)); OnChanged(nameof(Profit)); }
        }

        public decimal Total => Math.Round(Quantity * UnitPrice, 2); // المجموع

        /// <summary>سعر التكلفة لكل وحدة وقت البيع (نسخة محفوظة من السلعة، لا تتغير لاحقاً حتى لو تغيّر سعر التكلفة في المخزون).</summary>
        public decimal CostPrice
        {
            get => _costPrice;
            set { _costPrice = value; OnChanged(); OnChanged(nameof(Profit)); }
        }

        /// <summary>ربح هذا السطر = (سعر البيع - سعر التكلفة) × الكمية.</summary>
        public decimal Profit => Math.Round((UnitPrice - CostPrice) * Quantity, 2);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
