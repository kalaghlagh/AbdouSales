using System;

namespace AbdouSales.Models
{
    /// <summary>جدول الربط ActivityProducts(ActivityId, ProductId): علاقة متعدد-إلى-متعدد بين الأنشطة التجارية والسلع.</summary>
    public class ActivityProduct
    {
        public Guid ActivityId { get; set; }
        public Guid ProductId { get; set; }
    }
}
