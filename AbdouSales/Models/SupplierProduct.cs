using System;

namespace AbdouSales.Models
{
    /// <summary>
    /// جدول الربط Supplier ↔ Product (علاقة متعدد-إلى-متعدد) مع بيانات خاصة بكل ممون:
    /// سعر الشراء لدى هذا الممون، ورمز السلعة عنده، وتاريخ آخر شراء.
    /// الفريد: (SupplierId, ProductId) لا يتكرر أبداً.
    /// </summary>
    public class SupplierProduct
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SupplierId { get; set; }
        public Guid ProductId { get; set; }

        /// <summary>سعر الشراء لدى هذا الممون (null = غير محدد، فيُستعمل سعر التكلفة العام للسلعة).</summary>
        public decimal? PurchasePrice { get; set; }

        /// <summary>رمز السلعة في كتالوج الممون (اختياري).</summary>
        public string SupplierProductCode { get; set; } = string.Empty;

        public DateTime? LastPurchaseDate { get; set; }
    }
}
