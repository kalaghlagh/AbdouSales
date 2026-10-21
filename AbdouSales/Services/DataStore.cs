using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using AbdouSales.Models;

namespace AbdouSales.Services
{
    /// <summary>
    /// مخزن بيانات بسيط يعتمد على ملفات JSON محفوظة في مجلد بيانات التطبيق الخاص بالمستخدم.
    /// لا يحتاج أي قاعدة بيانات خارجية، مما يسهل تشغيل التطبيق على أي جهاز.
    /// </summary>
    public static class DataStore
    {
        private static readonly string RootFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AbdouSales");

        /// <summary>المجلد القديم من قبل إعادة تسمية التطبيق (كان يُسمى "NourInvoicing")، لنقل بيانات المستخدمين الحاليين تلقائياً.</summary>
        private static readonly string OldRootFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NourInvoicing");

        private static readonly string CustomersFile = Path.Combine(RootFolder, "customers.json");
        private static readonly string SuppliersFile = Path.Combine(RootFolder, "suppliers.json");
        private static readonly string ProductsFile = Path.Combine(RootFolder, "products.json");
        private static readonly string InvoicesFile = Path.Combine(RootFolder, "invoices.json");
        private static readonly string CompanyFile = Path.Combine(RootFolder, "company.json");
        private static readonly string ActivitiesFile = Path.Combine(RootFolder, "activities.json");
        private static readonly string ActivityProductsFile = Path.Combine(RootFolder, "activity_products.json");
        private static readonly string SupplierProductsFile = Path.Combine(RootFolder, "supplier_products.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static ObservableCollection<Customer> Customers { get; private set; } = new();
        public static ObservableCollection<Supplier> Suppliers { get; private set; } = new();
        public static ObservableCollection<Product> Products { get; private set; } = new();
        public static ObservableCollection<Invoice> Invoices { get; private set; } = new();
        public static CompanyInfo Company { get; private set; } = new();
        public static ObservableCollection<BusinessActivity> Activities { get; private set; } = new();

        /// <summary>قديم: ربط الأنشطة بالسلع. لم يعد يُستعمل للتصفية، يُقرأ مرة واحدة فقط لترحيله إلى SupplierProducts.</summary>
        public static ObservableCollection<ActivityProduct> ActivityProducts { get; private set; } = new();

        /// <summary>جدول الربط SupplierProducts: السلع التي يوفرها كل ممون مع سعر الشراء الخاص به.</summary>
        public static ObservableCollection<SupplierProduct> SupplierProducts { get; private set; } = new();

        /// <summary>
        /// عند أول تشغيل بعد إعادة تسمية التطبيق: إن وُجد مجلد بيانات قديم باسم "NourInvoicing"
        /// ولم يكن مجلد "AbdouSales" الجديد موجوداً بعد، نَنسخ ملفات JSON تلقائياً حتى لا يفقد
        /// المستخدم زبائنه وفواتيره ومخزونه السابق.
        /// </summary>
        private static void MigrateOldDataFolderIfNeeded()
        {
            try
            {
                if (Directory.Exists(RootFolder) || !Directory.Exists(OldRootFolder)) return;

                Directory.CreateDirectory(RootFolder);
                foreach (var oldFile in Directory.GetFiles(OldRootFolder, "*.json"))
                {
                    var destination = Path.Combine(RootFolder, Path.GetFileName(oldFile));
                    File.Copy(oldFile, destination, overwrite: false);
                }
            }
            catch
            {
                // إن فشل النقل لأي سبب، يبدأ التطبيق ببيانات فارغة بدل التعطل - لا خطر على البيانات القديمة نفسها.
            }
        }

        public static void Load()
        {
            MigrateOldDataFolderIfNeeded();
            Directory.CreateDirectory(RootFolder);

            Customers = LoadCollection<Customer>(CustomersFile);
            Suppliers = LoadCollection<Supplier>(SuppliersFile);
            Products = LoadCollection<Product>(ProductsFile);
            Invoices = LoadCollection<Invoice>(InvoicesFile);
            Company = LoadSingle<CompanyInfo>(CompanyFile) ?? new CompanyInfo();
            Activities = LoadCollection<BusinessActivity>(ActivitiesFile);
            ActivityProducts = LoadCollection<ActivityProduct>(ActivityProductsFile);

            bool supplierProductsFileExisted = File.Exists(SupplierProductsFile);
            SupplierProducts = LoadCollection<SupplierProduct>(SupplierProductsFile);

            MigrateSupplierActivitiesIfNeeded();
            if (!supplierProductsFileExisted) MigrateActivityLinksToSupplierProducts();
            CleanSupplierProducts();
        }

        /// <summary>
        /// ترحيل آمن (يُنفَّذ مرة واحدة عند عدم وجود supplier_products.json): كل سلعة كانت مرتبطة بنشاط
        /// تُربط بكل الممونين الذين لهم النشاط نفسه، بحيث لا يفقد المستخدم أي علاقة سابقة.
        /// سعر الشراء يُنسخ من سعر التكلفة العام للسلعة (إن كان أكبر من صفر). لا يُحذف أي ملف قديم.
        /// </summary>
        private static void MigrateActivityLinksToSupplierProducts()
        {
            bool changed = false;
            foreach (var link in ActivityProducts)
            {
                var product = Products.FirstOrDefault(p => p.Id == link.ProductId);
                if (product == null) continue;

                foreach (var supplier in Suppliers.Where(s => s.ActivityId == link.ActivityId))
                {
                    if (SupplierProducts.Any(sp => sp.SupplierId == supplier.Id && sp.ProductId == product.Id)) continue;
                    SupplierProducts.Add(new SupplierProduct
                    {
                        SupplierId = supplier.Id,
                        ProductId = product.Id,
                        PurchasePrice = product.CostPrice > 0 ? product.CostPrice : null
                    });
                    changed = true;
                }
            }
            if (changed || !File.Exists(SupplierProductsFile)) SaveSupplierProducts();
        }

        /// <summary>تنظيف عند التحميل: حذف الروابط اليتيمة (ممون أو سلعة محذوفة) والمكررة، ضماناً لقيد الفرادة.</summary>
        private static void CleanSupplierProducts()
        {
            var supplierIds = Suppliers.Select(s => s.Id).ToHashSet();
            var productIds = Products.Select(p => p.Id).ToHashSet();
            var seen = new System.Collections.Generic.HashSet<(Guid, Guid)>();

            var bad = SupplierProducts
                .Where(sp => !supplierIds.Contains(sp.SupplierId) || !productIds.Contains(sp.ProductId) || !seen.Add((sp.SupplierId, sp.ProductId)))
                .ToList();
            if (bad.Count == 0) return;

            foreach (var sp in bad) SupplierProducts.Remove(sp);
            SaveSupplierProducts();
        }

        /// <summary>
        /// ترحيل البيانات القديمة: الممونون الذين لديهم نشاط مكتوب كنص فقط (قبل إضافة الأنشطة) يُنشأ لهم
        /// نشاط مطابق في قائمة الأنشطة ويُربطون به عبر ActivityId، دون أي فقدان للبيانات.
        /// </summary>
        private static void MigrateSupplierActivitiesIfNeeded()
        {
            bool changed = false;
            foreach (var supplier in Suppliers)
            {
                if (supplier.ActivityId.HasValue) continue;
                if (string.IsNullOrWhiteSpace(supplier.Activity)) continue;

                supplier.ActivityId = GetOrCreateActivity(supplier.Activity).Id;
                changed = true;
            }
            if (changed) SaveSuppliers();
        }

        private static ObservableCollection<T> LoadCollection<T>(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<System.Collections.Generic.List<T>>(json, JsonOptions);
                    if (list != null) return new ObservableCollection<T>(list);
                }
            }
            catch
            {
                // في حال تلف الملف، نبدأ بقائمة فارغة بدل تعطل التطبيق
            }
            return new ObservableCollection<T>();
        }

        private static T? LoadSingle<T>(string path) where T : class
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<T>(json, JsonOptions);
                }
            }
            catch
            {
                // تجاهل الملف التالف
            }
            return null;
        }

        public static void SaveCustomers() => SaveCollection(Customers, CustomersFile);
        public static void SaveSuppliers() => SaveCollection(Suppliers, SuppliersFile);
        public static void SaveProducts() => SaveCollection(Products, ProductsFile);
        public static void SaveInvoices() => SaveCollection(Invoices, InvoicesFile);
        public static void SaveActivities() => SaveCollection(Activities, ActivitiesFile);
        public static void SaveActivityProducts() => SaveCollection(ActivityProducts, ActivityProductsFile);
        public static void SaveSupplierProducts() => SaveCollection(SupplierProducts, SupplierProductsFile);
        public static void SaveCompany() => File.WriteAllText(CompanyFile, JsonSerializer.Serialize(Company, JsonOptions));

        private static void SaveCollection<T>(ObservableCollection<T> collection, string path)
        {
            Directory.CreateDirectory(RootFolder);
            var json = JsonSerializer.Serialize(collection.ToList(), JsonOptions);
            File.WriteAllText(path, json);
        }

        public static void SaveAll()
        {
            SaveCustomers();
            SaveSuppliers();
            SaveProducts();
            SaveInvoices();
            SaveCompany();
            SaveActivities();
            SaveActivityProducts();
            SaveSupplierProducts();
        }

        // ===================== الأنشطة التجارية وربطها بالسلع =====================

        /// <summary>يعيد النشاط بهذا الاسم (مقارنة غير حساسة لحالة الأحرف)، وينشئه ويحفظه إن لم يكن موجوداً.</summary>
        public static BusinessActivity GetOrCreateActivity(string name)
        {
            name = (name ?? string.Empty).Trim();
            var existing = Activities.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var activity = new BusinessActivity { Name = name };
            Activities.Add(activity);
            SaveActivities();
            return activity;
        }

        // ===================== علاقة الممونين بالسلع (SupplierProducts) =====================

        public static SupplierProduct? GetSupplierProduct(Guid supplierId, Guid productId)
            => SupplierProducts.FirstOrDefault(sp => sp.SupplierId == supplierId && sp.ProductId == productId);

        public static bool IsProductLinkedToSupplier(Guid supplierId, Guid productId)
            => SupplierProducts.Any(sp => sp.SupplierId == supplierId && sp.ProductId == productId);

        /// <summary>سلع ممون معيّن فقط (تصفية على مستوى البيانات لا الواجهة)، مرتبة بالاسم. لا تُحمَّل بقية السلع.</summary>
        public static System.Collections.Generic.List<Product> GetProductsForSupplier(Guid supplierId)
        {
            var ids = SupplierProducts.Where(sp => sp.SupplierId == supplierId).Select(sp => sp.ProductId).ToHashSet();
            return Products.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public static System.Collections.Generic.List<SupplierProduct> GetLinksForProduct(Guid productId)
            => SupplierProducts.Where(sp => sp.ProductId == productId).ToList();

        /// <summary>
        /// ينشئ الرابط (ممون، سلعة) إن لم يوجد، أو يحدّث سعر الشراء/الرمز إن وُجد؛ لا ينتج أبداً رابطاً مكرراً.
        /// القيم null أو الفارغة لا تمسح قيمة موجودة. لا يحفظ الملف (استدعِ SaveSupplierProducts بعد الانتهاء).
        /// </summary>
        public static (SupplierProduct Link, bool Created, bool Changed) UpsertSupplierProduct(
            Guid supplierId, Guid productId, decimal? purchasePrice, string? supplierProductCode)
        {
            var link = GetSupplierProduct(supplierId, productId);
            bool created = false, changed = false;
            if (link == null)
            {
                link = new SupplierProduct { SupplierId = supplierId, ProductId = productId };
                SupplierProducts.Add(link);
                created = true;
            }

            if (purchasePrice.HasValue && link.PurchasePrice != purchasePrice) { link.PurchasePrice = purchasePrice; changed = true; }
            string code = (supplierProductCode ?? string.Empty).Trim();
            if (code.Length > 0 && link.SupplierProductCode != code) { link.SupplierProductCode = code; changed = true; }
            return (link, created, changed);
        }

        /// <summary>
        /// يجعل مجموعة ممونين السلعة مطابقة للقائمة المعطاة: يضيف/يحدّث الروابط المطلوبة ويحذف غير المطلوبة، ثم يحفظ.
        /// سعر الشراء null = بدون سعر خاص (فيُستعمل سعر التكلفة العام).
        /// </summary>
        public static void SetProductSuppliers(Guid productId,
            System.Collections.Generic.IEnumerable<(Guid SupplierId, decimal? PurchasePrice, string Code)> desired)
        {
            var wanted = desired.GroupBy(d => d.SupplierId).Select(g => g.First()).ToList();
            var wantedIds = wanted.Select(w => w.SupplierId).ToHashSet();

            foreach (var old in SupplierProducts.Where(sp => sp.ProductId == productId && !wantedIds.Contains(sp.SupplierId)).ToList())
                SupplierProducts.Remove(old);

            foreach (var (supplierId, price, code) in wanted)
            {
                var link = GetSupplierProduct(supplierId, productId);
                if (link == null)
                {
                    link = new SupplierProduct { SupplierId = supplierId, ProductId = productId };
                    SupplierProducts.Add(link);
                }
                link.PurchasePrice = price;                       // هنا التعديل صريح من المستخدم، فيُسمح بمسح السعر
                link.SupplierProductCode = (code ?? string.Empty).Trim();
            }
            SaveSupplierProducts();
        }

        public static void RemoveLinksForProduct(Guid productId)
        {
            var links = SupplierProducts.Where(sp => sp.ProductId == productId).ToList();
            if (links.Count == 0) return;
            foreach (var l in links) SupplierProducts.Remove(l);
            SaveSupplierProducts();
        }

        public static void RemoveLinksForSupplier(Guid supplierId)
        {
            var links = SupplierProducts.Where(sp => sp.SupplierId == supplierId).ToList();
            if (links.Count == 0) return;
            foreach (var l in links) SupplierProducts.Remove(l);
            SaveSupplierProducts();
        }

        /// <summary>مفتاح مقارنة اسم الممون في ترويسة الفاتورة: يتجاهل المسافات الزائدة وحالة الأحرف اللاتينية.</summary>
        public static string SupplierKey(string? name)
            => string.Join(' ', (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToUpperInvariant();

        /// <summary>
        /// يولّد رقم الفاتورة التالي بصيغة "السنة/التسلسل" (مثل 2026/001) <b>لكل ممون على حدة</b>:
        /// لكل اسم ممون في الترويسة عدّاده الخاص، ويبدأ من 001 في كل سنة. الفواتير بدون ممون (ترويسة الشركة) لها عدّاد مستقل.
        /// مبيعات الكاشير لا تدخل في العدّ. نأخذ أكبر تسلسل موجود +1 (وليس عدد الفواتير) حتى لا يتكرر رقم بعد حذف فاتورة.
        /// </summary>
        public static string GetNextInvoiceNumber(string? supplierName = null, DateTime? date = null)
        {
            int year = (date ?? DateTime.Now).Year;
            string key = SupplierKey(supplierName);
            string prefix = year.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/";

            var sameSupplier = Invoices.Where(i => !i.IsCashierSale && SupplierKey(i.SupplierName) == key).ToList();

            int maxSeq = 0;
            foreach (var invoice in sameSupplier)
            {
                var number = (invoice.Number ?? string.Empty).Trim();
                if (number.StartsWith(prefix, StringComparison.Ordinal)
                    && int.TryParse(number.AsSpan(prefix.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int seq)
                    && seq > maxSeq)
                    maxSeq = seq;
            }

            int next = maxSeq + 1;
            string candidate = $"{prefix}{next:000}";
            while (sameSupplier.Any(i => string.Equals((i.Number ?? string.Empty).Trim(), candidate, StringComparison.Ordinal)))
                candidate = $"{prefix}{++next:000}";
            return candidate;
        }

        /// <summary>يولّد رقم فاتورة تلقائي مخصص لمبيعات نقطة البيع (الكاشير)، بصيغة "POS-السنة-التسلسل".</summary>
        public static string GetNextCashierInvoiceNumber()
        {
            int year = DateTime.Now.Year;
            int nextSeq = Invoices.Count(i => i.Date.Year == year && i.IsCashierSale) + 1;
            return $"POS-{year}-{nextSeq:0000}";
        }

        /// <summary>يبحث عن سلعة عبر رمزها الشريطي (Barcode) بالمطابقة التامة.</summary>
        public static Product? FindProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            barcode = barcode.Trim();
            return Products.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Barcode)
                                                 && string.Equals(p.Barcode.Trim(), barcode, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>ينقص كمية المخزون للسلع المرتبطة بسطور فاتورة معينة (تُستدعى عند إتمام بيع)، ثم يحفظ التغيير.</summary>
        public static void DeductStockForInvoice(Invoice invoice)
        {
            bool changed = false;
            foreach (var line in invoice.Lines)
            {
                if (line.ProductId == Guid.Empty) continue;
                var product = Products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product == null) continue;
                product.QuantityInStock -= line.Quantity;
                changed = true;
            }
            if (changed) SaveProducts();
        }
    }
}
