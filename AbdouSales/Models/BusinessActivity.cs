using System;

namespace AbdouSales.Models
{
    /// <summary>نشاط تجاري (مثل: مواد البناء، المواد الغذائية...) يُربط به الممونون والسلع.</summary>
    public class BusinessActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
