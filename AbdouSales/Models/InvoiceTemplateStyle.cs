namespace AbdouSales.Models
{
    /// <summary>القوالب الجاهزة لتصميم الفاتورة المطبوعة / المُصدَّرة كـ PDF.</summary>
    public enum InvoiceTemplateStyle
    {
        /// <summary>كلاسيكي بسيط: أبيض وأسود، خطوط رفيعة، مناسب للطباعة الاقتصادية.</summary>
        ClassicClean = 0,

        /// <summary>احترافي كحلي (التصميم الأصلي للبرنامج).</summary>
        ExecutiveNavy = 1,

        /// <summary>عصري بالأزرق الملكي مع إبراز المجموع الكلي.</summary>
        ModernRoyalBlue = 2
    }
}
