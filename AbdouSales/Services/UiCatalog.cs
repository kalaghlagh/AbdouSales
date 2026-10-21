using System.Collections.Generic;

namespace AbdouSales.Services
{
    /// <summary>عنصر قابل للإخفاء: مفتاحه (UiVisibility.Key) ومفتاح نصه المترجم، مع لاحقة اختيارية للتمييز (ممون/زبون).</summary>
    public sealed record UiElementInfo(string Key, string LabelKey, string? SuffixKey = null);

    /// <summary>نافذة في البرنامج وعناصرها القابلة للإخفاء.</summary>
    public sealed record UiWindowInfo(string Id, string TitleKey, IReadOnlyList<UiElementInfo> Elements);

    /// <summary>
    /// فهرس العناصر التي يمكن التحكم في إظهارها وإخفائها من شاشة الإعدادات.
    /// يطابق الوسوم svc:UiVisibility.Key في ملفات XAML: عند إضافة زر جديد ضع عليه الوسم نفسه وأضفه هنا.
    /// (زر الإعدادات وشاشة الدخول لا يُدرجان حتى لا يُقفل المستخدم خارج البرنامج.)
    /// </summary>
    public static class UiCatalog
    {
        public static readonly IReadOnlyList<UiWindowInfo> Windows = new List<UiWindowInfo>
        {
            new UiWindowInfo("Main", "S198", new List<UiElementInfo>
            {
                new UiElementInfo("Main.LangSwitch", "S148"),
                new UiElementInfo("Main.NewInvoice", "S079"),
                new UiElementInfo("Main.Cashier", "S037"),
                new UiElementInfo("Main.Dashboard", "S129"),
                new UiElementInfo("Main.AddCustomer", "S001"),
                new UiElementInfo("Main.AddSupplier", "S033"),
                new UiElementInfo("Main.AddProduct", "S013"),
                new UiElementInfo("Main.TemplateChooser", "S080"),
                new UiElementInfo("Main.InvoiceList", "S081"),
            }),
            new UiWindowInfo("AddCustomer", "S001", new List<UiElementInfo>
            {
                new UiElementInfo("AddCustomer.ActivityBox", "S004"),
                new UiElementInfo("AddCustomer.AddressBox", "S005"),
                new UiElementInfo("AddCustomer.PhoneBox", "S006"),
                new UiElementInfo("AddCustomer.CommercialRegisterBox", "S007"),
                new UiElementInfo("AddCustomer.TaxIdBox", "S008"),
                new UiElementInfo("AddCustomer.Save_Click", "S009"),
                new UiElementInfo("AddCustomer.Delete_Click", "S010"),
                new UiElementInfo("AddCustomer.Clear_Click", "S011"),
                new UiElementInfo("AddCustomer.ImportExcel_Click", "S161"),
                new UiElementInfo("AddCustomer.ExcelTemplate_Click", "S162"),
                new UiElementInfo("AddCustomer.Close_Click", "S012"),
            }),
            new UiWindowInfo("AddSupplier", "S033", new List<UiElementInfo>
            {
                new UiElementInfo("AddSupplier.ActivityBox", "S004"),
                new UiElementInfo("AddSupplier.AddressBox", "S005"),
                new UiElementInfo("AddSupplier.PhoneBox", "S006"),
                new UiElementInfo("AddSupplier.CommercialRegisterBox", "S007"),
                new UiElementInfo("AddSupplier.TaxIdBox", "S008"),
                new UiElementInfo("AddSupplier.ArticleNumberBox", "S099"),
                new UiElementInfo("AddSupplier.Save_Click", "S036"),
                new UiElementInfo("AddSupplier.Delete_Click", "S010"),
                new UiElementInfo("AddSupplier.Clear_Click", "S011"),
                new UiElementInfo("AddSupplier.ImportExcel_Click", "S161"),
                new UiElementInfo("AddSupplier.ExcelTemplate_Click", "S162"),
                new UiElementInfo("AddSupplier.Close_Click", "S012"),
            }),
            new UiWindowInfo("AddProduct", "S013", new List<UiElementInfo>
            {
                new UiElementInfo("AddProduct.UnitBox", "S016"),
                new UiElementInfo("AddProduct.BarcodeBox", "S032"),
                new UiElementInfo("AddProduct.GenerateBarcode_Click", "S027"),
                new UiElementInfo("AddProduct.CostPriceBox", "S153"),
                new UiElementInfo("AddProduct.SuppliersLinkGrid", "S158"),
                new UiElementInfo("AddProduct.StockBox", "S028"),
                new UiElementInfo("AddProduct.Save_Click", "S029"),
                new UiElementInfo("AddProduct.Delete_Click", "S010"),
                new UiElementInfo("AddProduct.AssignSupplier_Click", "S183"),
                new UiElementInfo("AddProduct.Clear_Click", "S011"),
                new UiElementInfo("AddProduct.ImportExcel_Click", "S161"),
                new UiElementInfo("AddProduct.ExcelTemplate_Click", "S162"),
                new UiElementInfo("AddProduct.SelectAll_Click", "S181"),
                new UiElementInfo("AddProduct.SelectNone_Click", "S182"),
                new UiElementInfo("AddProduct.Close_Click", "S012"),
            }),
            new UiWindowInfo("NewInvoice", "S079", new List<UiElementInfo>
            {
                new UiElementInfo("NewInvoice.SupplierActivityBox", "S004", "S185"),
                new UiElementInfo("NewInvoice.SupplierAddressBox", "S005", "S185"),
                new UiElementInfo("NewInvoice.SupplierCommercialRegisterBox", "S007", "S185"),
                new UiElementInfo("NewInvoice.SupplierTaxIdBox", "S008", "S185"),
                new UiElementInfo("NewInvoice.SupplierArticleNumberBox", "S099", "S185"),
                new UiElementInfo("NewInvoice.CustomerActivityBox", "S004", "S186"),
                new UiElementInfo("NewInvoice.CustomerAddressBox", "S005", "S186"),
                new UiElementInfo("NewInvoice.CustomerCommercialRegisterBox", "S007", "S186"),
                new UiElementInfo("NewInvoice.CustomerTaxIdBox", "S008", "S186"),
                new UiElementInfo("NewInvoice.AddLineFromStock_Click", "S085"),
                new UiElementInfo("NewInvoice.AddManualLine_Click", "S086"),
                new UiElementInfo("NewInvoice.RemoveLine_Click", "S087"),
                new UiElementInfo("NewInvoice.PaymentMethodCombo", "S088"),
                new UiElementInfo("NewInvoice.VatRateBox", "S091"),
                new UiElementInfo("NewInvoice.FiscalStampBox", "S092"),
                new UiElementInfo("NewInvoice.IsPaidCheckBox", "S147"),
                new UiElementInfo("NewInvoice.SaveInvoice_Click", "S093"),
                new UiElementInfo("NewInvoice.PrintPreview_Click", "S094"),
                new UiElementInfo("NewInvoice.Cancel_Click", "S095"),
            }),
            new UiWindowInfo("Cashier", "S037", new List<UiElementInfo>
            {
                new UiElementInfo("Cashier.ScanQuantityBox", "S040"),
                new UiElementInfo("Cashier.ProductPickerCombo", "S041"),
                new UiElementInfo("Cashier.FocusBarcode_Click", "S042"),
                new UiElementInfo("Cashier.PaymentMethodCombo", "S048"),
                new UiElementInfo("Cashier.VatRateBox", "S052"),
                new UiElementInfo("Cashier.AmountReceivedBox", "S056"),
                new UiElementInfo("Cashier.Checkout_Click", "S058"),
                new UiElementInfo("Cashier.SaveOnly_Click", "S059"),
                new UiElementInfo("Cashier.ClearCart_Click", "S060"),
                new UiElementInfo("Cashier.Close_Click", "S012"),
            }),
            new UiWindowInfo("Dashboard", "S129", new List<UiElementInfo>
            {
                new UiElementInfo("Dashboard.Refresh_Click", "S141"),
                new UiElementInfo("Dashboard.CardRevenue", "S131"),
                new UiElementInfo("Dashboard.CardProfit", "S132"),
                new UiElementInfo("Dashboard.CardUnpaid", "S133"),
                new UiElementInfo("Dashboard.CardLowStock", "S135"),
                new UiElementInfo("Dashboard.Close_Click", "S012"),
            }),
            new UiWindowInfo("InvoiceList", "S061", new List<UiElementInfo>
            {
                new UiElementInfo("InvoiceList.PrintSelected_Click", "S067"),
                new UiElementInfo("InvoiceList.ShareSelected_Click", "S068"),
                new UiElementInfo("InvoiceList.DeleteSelected_Click", "S069"),
                new UiElementInfo("InvoiceList.Close_Click", "S012"),
            }),
            new UiWindowInfo("InvoicePrint", "S070", new List<UiElementInfo>
            {
                new UiElementInfo("InvoicePrint.ChangeTemplate_Click", "S071"),
                new UiElementInfo("InvoicePrint.Print_Click", "S072"),
                new UiElementInfo("InvoicePrint.Share_Click", "S073"),
                new UiElementInfo("InvoicePrint.Close_Click", "S012"),
            }),
            new UiWindowInfo("ShareInvoice", "S102", new List<UiElementInfo>
            {
                new UiElementInfo("ShareInvoice.OpenFolder_Click", "S105"),
                new UiElementInfo("ShareInvoice.OpenPdf_Click", "S106"),
                new UiElementInfo("ShareInvoice.SaveAs_Click", "S107"),
                new UiElementInfo("ShareInvoice.PhoneBox", "S108"),
                new UiElementInfo("ShareInvoice.EmailBox", "S110"),
                new UiElementInfo("ShareInvoice.WhatsApp_Click", "S112"),
                new UiElementInfo("ShareInvoice.Gmail_Click", "S113"),
                new UiElementInfo("ShareInvoice.CopyOnly_Click", "S115"),
                new UiElementInfo("ShareInvoice.Close_Click", "S012"),
            }),
            new UiWindowInfo("TemplateChooser", "S080", new List<UiElementInfo>
            {
                new UiElementInfo("TemplateChooser.SetAsDefaultCheck", "S127"),
                new UiElementInfo("TemplateChooser.Cancel_Click", "S095"),
            }),
        };
    }
}
