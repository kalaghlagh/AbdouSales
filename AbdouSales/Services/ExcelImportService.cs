using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AbdouSales.Models;

namespace AbdouSales.Services
{
    /// <summary>نتيجة عملية الاستيراد، تُعرض للمستخدم بعد الانتهاء.</summary>
    public class ImportResult
    {
        public int Added { get; set; }
        public int Duplicates { get; set; }
        public int Linked { get; set; }   // روابط ممون↔سلعة جديدة (SupplierProducts)
        public int Updated { get; set; }  // أسعار شراء/رموز تم تحديثها في روابط موجودة
        public int Failed => Errors.Count;
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public string? FatalError { get; set; }

        public string ToMessage(int maxLines = 12)
        {
            if (FatalError != null) return FatalError;

            var sb = new StringBuilder();
            sb.AppendLine($"✔ تمت إضافة: {Added}");
            sb.AppendLine($"↷ تم تجاهل المكرر (موجود مسبقاً): {Duplicates}");
            if (Linked > 0 || Updated > 0)
            {
                sb.AppendLine($"🔗 روابط ممون↔سلعة جديدة: {Linked}");
                sb.AppendLine($"✎ روابط تم تحديث سعر الشراء/الرمز فيها: {Updated}");
            }
            sb.AppendLine($"✖ صفوف مرفوضة: {Failed}");

            if (Errors.Count > 0)
            {
                sb.AppendLine().AppendLine("الأخطاء:");
                foreach (var e in Errors.Take(maxLines)) sb.AppendLine("• " + e);
                if (Errors.Count > maxLines) sb.AppendLine($"… و {Errors.Count - maxLines} أخطاء أخرى.");
            }
            if (Warnings.Count > 0)
            {
                sb.AppendLine().AppendLine("تنبيهات:");
                foreach (var w in Warnings.Take(maxLines)) sb.AppendLine("• " + w);
                if (Warnings.Count > maxLines) sb.AppendLine($"… و {Warnings.Count - maxLines} تنبيهات أخرى.");
            }
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// استيراد الممونين والزبائن والسلع من ملف Excel (.xlsx).
    /// الصف الأول = أسماء الأعمدة (بالعربية أو الفرنسية أو الإنجليزية، والترتيب غير مهم). القيم تُحفظ كما كُتبت دون أي تعديل.
    /// السجلات الموجودة مسبقاً لا تُعدَّل ولا تُحذف؛ المكررة فقط تُتجاهل.
    /// </summary>
    public static class ExcelImportService
    {
        // ------------------------------ تعريف الأعمدة وأسمائها المقبولة ------------------------------

        private const string FName = "name", FActivity = "activity", FAddress = "address", FPhone = "phone",
            FRegister = "register", FTaxId = "taxid", FArticle = "article",
            FUnit = "unit", FPrice = "price", FCost = "cost", FStock = "stock", FBarcode = "barcode",
            FSupplierRegister = "supplierregister", FSupplierName = "suppliername", FSupplierId = "supplierid",
            FPurchase = "purchase", FSupplierCode = "suppliercode";

        private static readonly Dictionary<string, string[]> Aliases = new()
        {
            [FName] = new[] { "name", "nom", "designation", "الاسم", "اسم", "اسمالممون", "اسمالزبون", "اسمالسلعه", "التعيين", "تعيين", "السلعه", "الممون", "الزبون", "produit", "fournisseur", "client" },
            [FActivity] = new[] { "activity", "activite", "النشاط", "نشاط", "النشاطالتجاري" },
            [FAddress] = new[] { "address", "adresse", "العنوان", "عنوان" },
            [FPhone] = new[] { "phone", "tel", "telephone", "mobile", "الهاتف", "هاتف", "رقمالهاتف" },
            [FRegister] = new[] { "commercialregister", "registrecommerce", "registredecommerce", "rc", "crn", "السجلالتجاري", "رقمالسجلالتجاري", "سجلتجاري", "السجل" },
            [FTaxId] = new[] { "taxid", "nif", "tin", "numerofiscal", "الرقمالجبائي", "رقمجبائي", "الرقمالضريبي", "الرقمالجبايي" },
            [FArticle] = new[] { "articlenumber", "numarticle", "numerodarticle", "nart", "رقمالماده", "رقمالمادة", "رقمالمادّة" },
            [FUnit] = new[] { "unit", "unite", "الوحده", "وحده", "وحدهالقياس" },
            [FPrice] = new[] { "unitprice", "price", "prixunitaire", "prixvente", "prix", "سعرالوحده", "سعرالبيع", "السعر", "سعر" },
            [FCost] = new[] { "costprice", "cost", "prixrevient", "سعرالتكلفه", "التكلفه" },
            [FStock] = new[] { "stock", "quantity", "qty", "quantite", "quantiteenstock", "الكميه", "الكميهالمتوفره", "المخزون", "الكميهفيالمخزون", "كميه" },
            [FSupplierRegister] = new[] { "supplierregister", "suppliercommercialregister", "supplierrc", "rcfournisseur", "registrecommercefournisseur", "السجلالتجاريللممون", "سجلالممون", "سجلتجاريللممون" },
            [FSupplierName] = new[] { "supplier", "suppliername", "fournisseur", "nomfournisseur", "الممون", "اسمالممون" },
            [FSupplierId] = new[] { "supplierid", "idfournisseur", "معرفالممون" },
            [FPurchase] = new[] { "purchaseprice", "supplierprice", "prixachat", "prixdachat", "prixachatfournisseur", "سعرالشراء", "سعرالشراءمنالممون", "سعرالشراءلدىالممون" },
            [FSupplierCode] = new[] { "suppliercode", "supplierproductcode", "codefournisseur", "رمزالسلعهعندالممون", "رمزالممون", "كودالممون" },
            [FBarcode] = new[] { "barcode", "codebarres", "codebarre", "ean", "الباركود", "باركود", "الرمزالشريطي", "رمزشريطي" },
        };

        /// <summary>تطبيع اسم العمود: أحرف صغيرة، حذف المسافات والرموز والتشكيل، وتوحيد الألفات والياء والتاء المربوطة.</summary>
        private static string NormalizeHeader(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char raw in text.Normalize(NormalizationForm.FormD))
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(raw);
                if (cat == UnicodeCategory.NonSpacingMark) continue;               // تشكيل / علامات الحروف
                if (!char.IsLetterOrDigit(raw)) continue;                            // مسافات، شرطات، نقاط، (د.ج)...
                char ch = char.ToLowerInvariant(raw);
                ch = ch switch { 'أ' or 'إ' or 'آ' => 'ا', 'ى' => 'ي', 'ة' => 'ه', _ => ch };
                sb.Append(ch);
            }
            return sb.ToString();
        }

        // في ورقة السلع: عمود "الممون" يخص الممون وليس اسم السلعة، فنستعمل أسماء مخصصة لعمود التعيين
        private static readonly Dictionary<string, string[]> ProductAliasOverrides = new()
        {
            [FName] = new[] { "name", "nom", "designation", "productname", "produit", "الاسم", "اسم", "اسمالسلعه", "التعيين", "تعيين", "السلعه" }
        };

        private static Dictionary<string, int> MapColumns(string[] headerRow, Dictionary<string, string[]>? overrides, params string[] wantedFields)
        {
            var map = new Dictionary<string, int>();
            foreach (var field in wantedFields)
            {
                var source = overrides != null && overrides.TryGetValue(field, out var o) ? o : Aliases[field];
                var names = source.Select(NormalizeHeader).ToHashSet();
                for (int i = 0; i < headerRow.Length; i++)
                {
                    if (map.ContainsValue(i)) continue;
                    if (names.Contains(NormalizeHeader(headerRow[i]))) { map[field] = i; break; }
                }
            }
            return map;
        }

        // ------------------------------ قراءة الملف ------------------------------

        private sealed class Sheet
        {
            public Dictionary<string, int> Columns { get; init; } = new();
            public List<(int RowNumber, string[] Cells)> Rows { get; } = new();
        }

        private static Sheet? LoadSheet(string path, ImportResult result, string[] wantedFields, Dictionary<string, string[]>? overrides = null)
        {
            List<string[]> all;
            try { all = ExcelHelper.ReadFirstSheet(path); }
            catch (Exception ex) when (ex is System.IO.InvalidDataException || ex is System.Xml.XmlException)
            {
                result.FatalError = "تعذرت قراءة الملف. تأكد أنه ملف Excel بصيغة .xlsx (وليس .xls القديم) ثم أعد المحاولة.";
                return null;
            }
            catch (System.IO.IOException ex)
            {
                result.FatalError = "تعذر فتح الملف: " + ex.Message;
                return null;
            }

            int headerIndex = all.FindIndex(r => r.Any(c => !string.IsNullOrWhiteSpace(c)));
            if (headerIndex < 0)
            {
                result.FatalError = "الملف فارغ.";
                return null;
            }

            var sheet = new Sheet { Columns = MapColumns(all[headerIndex], overrides, wantedFields) };
            if (!sheet.Columns.ContainsKey(FName))
            {
                result.FatalError = "لم يُعثر على عمود الاسم في الصف الأول. استعمل «تحميل نموذج Excel» للحصول على الأعمدة الصحيحة.";
                return null;
            }

            for (int i = headerIndex + 1; i < all.Count; i++)
            {
                if (all[i].All(string.IsNullOrWhiteSpace)) continue; // تخطي الصفوف الفارغة
                sheet.Rows.Add((i + 1, all[i]));
            }
            return sheet;
        }

        private static string Get(Sheet sheet, string[] cells, string field)
            => sheet.Columns.TryGetValue(field, out int idx) && idx < cells.Length ? (cells[idx] ?? string.Empty).Trim() : string.Empty;

        // يقبل 1250.50 و 1250,50 و "1 250,50" و "1,250.50"
        private static bool TryParseNumber(string text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Replace(" ", "").Replace("\u00A0", "");
            if (t.Contains(',') && t.Contains('.')) t = t.Replace(",", "");
            else t = t.Replace(',', '.');
            return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string Key(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        // ------------------------------ الممونون ------------------------------

        public static ImportResult ImportSuppliers(string path)
        {
            var result = new ImportResult();
            var sheet = LoadSheet(path, result, new[] { FName, FActivity, FAddress, FPhone, FRegister, FTaxId, FArticle });
            if (sheet == null) return result;

            foreach (var (rowNo, cells) in sheet.Rows)
            {
                string name = Get(sheet, cells, FName);
                string activityName = Get(sheet, cells, FActivity);
                string register = Get(sheet, cells, FRegister);

                if (name.Length == 0) { result.Errors.Add($"السطر {rowNo}: الاسم فارغ."); continue; }

                bool duplicate = DataStore.Suppliers.Any(s =>
                    register.Length > 0 && s.CommercialRegister.Trim().Length > 0
                        ? string.Equals(s.CommercialRegister.Trim(), register, StringComparison.Ordinal)
                        : Key(s.Name) == Key(name));
                if (duplicate) { result.Duplicates++; continue; }

                var activity = activityName.Length > 0 ? DataStore.GetOrCreateActivity(activityName) : null; // النشاط اختياري (وصف فقط)
                DataStore.Suppliers.Add(new Supplier
                {
                    Name = name,
                    Activity = activity?.Name ?? string.Empty,
                    ActivityId = activity?.Id,
                    Address = Get(sheet, cells, FAddress),
                    Phone = Get(sheet, cells, FPhone),
                    CommercialRegister = register,
                    TaxId = Get(sheet, cells, FTaxId),
                    ArticleNumber = Get(sheet, cells, FArticle)
                });
                result.Added++;
            }

            if (result.Added > 0) DataStore.SaveSuppliers();
            return result;
        }

        // ------------------------------ الزبائن ------------------------------

        public static ImportResult ImportCustomers(string path)
        {
            var result = new ImportResult();
            var sheet = LoadSheet(path, result, new[] { FName, FActivity, FAddress, FPhone, FRegister, FTaxId });
            if (sheet == null) return result;

            foreach (var (rowNo, cells) in sheet.Rows)
            {
                string name = Get(sheet, cells, FName);
                string register = Get(sheet, cells, FRegister);

                if (name.Length == 0) { result.Errors.Add($"السطر {rowNo}: الاسم فارغ."); continue; }

                bool duplicate = DataStore.Customers.Any(c =>
                    register.Length > 0 && c.CommercialRegister.Trim().Length > 0
                        ? string.Equals(c.CommercialRegister.Trim(), register, StringComparison.Ordinal)
                        : Key(c.Name) == Key(name));
                if (duplicate) { result.Duplicates++; continue; }

                DataStore.Customers.Add(new Customer
                {
                    Name = name,
                    Activity = Get(sheet, cells, FActivity),
                    Address = Get(sheet, cells, FAddress),
                    Phone = Get(sheet, cells, FPhone),
                    CommercialRegister = register,
                    TaxId = Get(sheet, cells, FTaxId)
                });
                result.Added++;
            }

            if (result.Added > 0) DataStore.SaveCustomers();
            return result;
        }

        // ------------------------------ السلع ------------------------------

        /// <summary>
        /// استيراد السلع وربطها بالممونين (SupplierProducts):
        /// السلعة تُعرَّف بالباركود (نص كما هو، دون تحويل رقمي)، وإن لم يوجد باركود فبالاسم؛ لا تُنشأ سلعة مكررة أبداً.
        /// الممون يُعرَّف بمعرّفه (Id) ثم بسجله التجاري ثم باسمه. الربط لا يتكرر، وسعر الشراء يُحدَّث إن وُجد في الملف.
        /// </summary>
        public static ImportResult ImportProducts(string path)
        {
            var result = new ImportResult();
            var sheet = LoadSheet(path, result,
                new[] { FName, FUnit, FPrice, FCost, FStock, FBarcode, FSupplierRegister, FSupplierName, FSupplierId, FPurchase, FSupplierCode },
                ProductAliasOverrides);
            if (sheet == null) return result;

            bool hasSupplierColumn = sheet.Columns.ContainsKey(FSupplierRegister) || sheet.Columns.ContainsKey(FSupplierName) || sheet.Columns.ContainsKey(FSupplierId);
            if (!hasSupplierColumn)
                result.Warnings.Add("لا يوجد عمود للممون (السجل التجاري أو الاسم): السلع المستوردة لن تظهر في فواتير أي ممون حتى تربطها به.");

            bool productsChanged = false, linksChanged = false;

            foreach (var (rowNo, cells) in sheet.Rows)
            {
                string name = Get(sheet, cells, FName);
                if (name.Length == 0) { result.Errors.Add($"السطر {rowNo}: التعيين فارغ."); continue; }

                // الأرقام: القيمة الفارغة = 0، والقيمة غير الصالحة أو السالبة = رفض السطر
                if (!ParseOptionalAmount(Get(sheet, cells, FPrice), out decimal price) || price < 0)
                { result.Errors.Add($"السطر {rowNo} ({name}): سعر البيع غير صالح."); continue; }
                if (!ParseOptionalAmount(Get(sheet, cells, FCost), out decimal cost) || cost < 0)
                { result.Errors.Add($"السطر {rowNo} ({name}): سعر التكلفة غير صالح."); continue; }
                if (!ParseOptionalAmount(Get(sheet, cells, FStock), out decimal stock) || stock < 0)
                { result.Errors.Add($"السطر {rowNo} ({name}): الكمية غير صالحة."); continue; }

                decimal? purchasePrice = null;
                string purchaseText = Get(sheet, cells, FPurchase);
                if (purchaseText.Length > 0)
                {
                    if (!TryParseNumber(purchaseText, out decimal pp) || pp < 0)
                    { result.Errors.Add($"السطر {rowNo} ({name}): سعر الشراء غير صالح."); continue; }
                    purchasePrice = pp;
                }

                // ----- السلعة: إيجاد الموجودة أو إنشاء جديدة -----
                string barcode = Get(sheet, cells, FBarcode); // نص دائماً
                Product? product = barcode.Length > 0
                    ? DataStore.FindProductByBarcode(barcode)
                    : DataStore.Products.FirstOrDefault(p => Key(p.Name) == Key(name));

                bool createdProduct = false;
                if (product == null)
                {
                    string unit = Get(sheet, cells, FUnit);
                    product = new Product
                    {
                        Name = name,
                        Unit = unit.Length > 0 ? unit : "قطعة",
                        UnitPrice = price,
                        CostPrice = cost,
                        QuantityInStock = stock,
                        Barcode = barcode
                    };
                    DataStore.Products.Add(product);
                    createdProduct = true;
                    productsChanged = true;
                }

                // ----- الممون والربط -----
                bool linkedNow = false, updatedNow = false;
                string supplierId = Get(sheet, cells, FSupplierId);
                string supplierRegister = Get(sheet, cells, FSupplierRegister);
                string supplierName = Get(sheet, cells, FSupplierName);

                if (supplierId.Length > 0 || supplierRegister.Length > 0 || supplierName.Length > 0)
                {
                    var supplier = FindSupplier(supplierId, supplierRegister, supplierName);
                    if (supplier == null)
                    {
                        string shown = supplierRegister.Length > 0 ? supplierRegister : supplierName.Length > 0 ? supplierName : supplierId;
                        result.Warnings.Add($"السطر {rowNo} ({name}): الممون \"{shown}\" غير موجود، لم يتم الربط.");
                    }
                    else
                    {
                        var (_, created, changed) = DataStore.UpsertSupplierProduct(supplier.Id, product.Id, purchasePrice, Get(sheet, cells, FSupplierCode));
                        linkedNow = created;
                        updatedNow = !created && changed;
                        if (created) result.Linked++;
                        if (updatedNow) result.Updated++;
                        linksChanged |= created || changed;
                    }
                }

                if (createdProduct) result.Added++;
                else if (!linkedNow && !updatedNow) result.Duplicates++;
            }

            if (productsChanged) DataStore.SaveProducts();
            if (linksChanged) DataStore.SaveSupplierProducts();
            return result;
        }

        /// <summary>يبحث عن الممون بمعرّفه (Guid)، ثم بسجله التجاري، ثم باسمه (مقارنة غير حساسة لحالة الأحرف).</summary>
        private static Supplier? FindSupplier(string id, string register, string name)
        {
            if (id.Length > 0 && Guid.TryParse(id, out var guid))
            {
                var byId = DataStore.Suppliers.FirstOrDefault(s => s.Id == guid);
                if (byId != null) return byId;
            }
            if (register.Length > 0)
            {
                var byRegister = DataStore.Suppliers.FirstOrDefault(s =>
                    string.Equals(s.CommercialRegister?.Trim(), register, StringComparison.Ordinal));
                if (byRegister != null) return byRegister;
            }
            if (name.Length > 0) return DataStore.Suppliers.FirstOrDefault(s => Key(s.Name) == Key(name));
            return null;
        }

        private static bool ParseOptionalAmount(string text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return true;
            return TryParseNumber(text, out value);
        }
    }
}
