using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AbdouSales.Models
{
    public class Product : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;       // التعيين (اسم السلعة)
        public string Unit { get; set; } = "قطعة";              // وحدة القياس: كيس، رولو، متر...
        public decimal UnitPrice { get; set; }                  // سعر الوحدة (سعر البيع)
        public decimal CostPrice { get; set; }                  // سعر التكلفة (سعر الشراء) - لحساب الربح
        public decimal QuantityInStock { get; set; }            // الكمية المتوفرة في المخزون
        public string Barcode { get; set; } = string.Empty;     // الرمز الشريطي (Code-barres) لاستعماله مع قارئ الباركود

        /// <summary>حالة التحديد في قائمة السلع (واجهة فقط، لا تُحفظ في ملف JSON).</summary>
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public override string ToString() => Name;
    }
}
