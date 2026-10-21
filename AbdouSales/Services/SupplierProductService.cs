using System;
using System.Collections.Generic;
using System.Linq;
using AbdouSales.Models;

namespace AbdouSales.Services
{
    /// <summary>
    /// منطق العمل لعلاقة Supplier → SupplierProduct ← Product. تستعمله الواجهة (تصفية القائمة وجلب السعر)
    /// وكذلك التحقق عند الحفظ، بحيث لا يمكن تجاوز القيد حتى لو تغيّرت الواجهة.
    /// القاعدة: السلعة مسموحة في فاتورة ممون فقط إذا وُجد له سجل SupplierProduct بها.
    /// </summary>
    public static class SupplierProductService
    {
        /// <summary>
        /// هل يُسمح بإضافة سطور يدوية (بدون سلعة من المخزون)؟ هذه السطور لا تحمل ProductId لذلك لا يمكن فحص علاقتها.
        /// اجعلها false لمنعها كلياً.
        /// </summary>
        public const bool AllowManualLines = true;

        /// <summary>سلع الممون فقط، مرتبة بالاسم. قائمة فارغة إن لم يُختر ممون.</summary>
        public static List<Product> GetProductsForSupplier(Supplier? supplier)
            => supplier == null ? new List<Product>() : DataStore.GetProductsForSupplier(supplier.Id);

        public static bool IsLinked(Supplier? supplier, Guid productId)
            => supplier != null && DataStore.IsProductLinkedToSupplier(supplier.Id, productId);

        /// <summary>
        /// سعر الشراء الفعلي: السعر الخاص بالممون إن وُجد، وإلا سعر التكلفة العام للسلعة.
        /// لا يُستعمل السعر العام إذا كان للممون سعر خاص.
        /// </summary>
        public static decimal GetPurchasePrice(Supplier supplier, Product product)
            => DataStore.GetSupplierProduct(supplier.Id, product.Id)?.PurchasePrice ?? product.CostPrice;

        /// <summary>يتحقق من كل سطور الفاتورة ويعيد قائمة بالأخطاء (فارغة = الفاتورة صحيحة).</summary>
        public static List<string> ValidateInvoice(Invoice invoice)
        {
            var errors = new List<string>();
            var supplier = invoice.SupplierId == Guid.Empty
                ? null
                : DataStore.Suppliers.FirstOrDefault(s => s.Id == invoice.SupplierId);

            foreach (var line in invoice.Lines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    if (!AllowManualLines)
                        errors.Add($"السطر \"{line.ProductName}\" يدوي وغير مسموح به.");
                    continue;
                }

                if (supplier == null)
                    errors.Add($"السلعة \"{line.ProductName}\" تتطلب اختيار ممون من القائمة.");
                else if (!IsLinked(supplier, line.ProductId))
                    errors.Add($"السلعة \"{line.ProductName}\" غير مرتبطة بالممون \"{supplier.Name}\".");
            }
            return errors;
        }
    }
}
