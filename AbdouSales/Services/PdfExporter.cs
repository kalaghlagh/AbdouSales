using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AbdouSales.Models;

namespace AbdouSales.Services
{
    /// <summary>
    /// يحوّل الفاتورة إلى ملف PDF حقيقي بدون أي مكتبات خارجية أو حزم NuGet.
    /// الفكرة: نرسم كل صفحة من الفاتورة (بنفس تصميم الطباعة تماماً مع الإطار)
    /// كصورة عالية الدقة، ثم نكتب ملف PDF قياسي يحتوي هذه الصفحات.
    /// الميزة: النصوص العربية تظهر بشكل صحيح 100% على أي جهاز أو هاتف،
    /// لأن الرسم يتم بواسطة ويندوز وليس باستخدام خطوط مدمجة.
    /// </summary>
    public static class PdfExporter
    {
        /// <summary>دقة التصدير (2 = حوالي 192 نقطة/بوصة، جودة ممتازة وحجم معقول).</summary>
        private const double RenderScale = 2.0;

        /// <summary>جودة ضغط الصورة داخل ملف PDF.</summary>
        private const int JpegQuality = 88;

        /// <summary>أبعاد صفحة A4 بوحدة النقاط (points) المستعملة داخل ملفات PDF.</summary>
        private const double A4WidthPt = 595.276;
        private const double A4HeightPt = 841.890;

        /// <summary>المجلد الافتراضي الذي تُحفظ فيه فواتير PDF.</summary>
        public static string DefaultFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "فواتير AbdouSales");

        /// <summary>يبني اسم ملف مناسب للفاتورة (بدون رموز ممنوعة في ويندوز).</summary>
        public static string BuildFileName(Invoice invoice)
        {
            string number = Sanitize(invoice.Number);
            string customer = Sanitize(invoice.CustomerName);

            string name = string.IsNullOrWhiteSpace(customer)
                ? $"فاتورة {number}"
                : $"فاتورة {number} - {customer}";

            if (name.Length > 90) name = name.Substring(0, 90);
            return name + ".pdf";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in value.Trim())
            {
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '-' : c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// ينشئ ملف PDF للفاتورة في المجلد الافتراضي ويعيد مسار الملف الكامل.
        /// </summary>
        public static string ExportInvoice(Invoice invoice, CompanyInfo company)
        {
            Directory.CreateDirectory(DefaultFolder);
            string path = Path.Combine(DefaultFolder, BuildFileName(invoice));

            // إذا كان الملف مفتوحاً أو محجوزاً، نستعمل اسماً بديلاً بدل تعطل البرنامج.
            if (File.Exists(path) && IsFileLocked(path))
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                path = Path.Combine(DefaultFolder, $"{stem} ({DateTime.Now:HHmmss}).pdf");
            }

            var document = InvoiceDocumentBuilder.Build(invoice, company);
            ExportDocument(document, path);
            return path;
        }

        /// <summary>يحوّل مستند فاتورة جاهز إلى ملف PDF في المسار المحدد.</summary>
        public static void ExportDocument(FlowDocument document, string outputPath)
        {
            var pages = RenderPages(document);
            if (pages.Count == 0)
                throw new InvalidOperationException("تعذر توليد صفحات الفاتورة.");

            string? folder = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            WritePdf(stream, pages);
        }

        private static bool IsFileLocked(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  1) رسم صفحات الفاتورة كصور (بنفس مظهر الطباعة)
        // ============================================================

        private sealed class RenderedPage
        {
            public byte[] JpegBytes { get; set; } = Array.Empty<byte>();
            public int PixelWidth { get; set; }
            public int PixelHeight { get; set; }
        }

        private static List<RenderedPage> RenderPages(FlowDocument source)
        {
            var result = new List<RenderedPage>();

            // نعمل على نسخة حتى لا نغيّر المستند المعروض في نافذة المعاينة.
            var doc = CloneDocument(source);

            double width = InvoiceDocumentBuilder.A4WidthPx;
            double height = InvoiceDocumentBuilder.A4HeightPx;

            doc.PageWidth = width;
            doc.PageHeight = height;
            doc.PagePadding = new Thickness(InvoiceDocumentBuilder.PageMarginPx);
            doc.ColumnWidth = double.PositiveInfinity;

            IDocumentPaginatorSource paginatorSource = doc;
            var paginator = paginatorSource.DocumentPaginator;
            paginator.PageSize = new Size(width, height);
            paginator.ComputePageCount();

            for (int i = 0; i < paginator.PageCount; i++)
            {
                var page = paginator.GetPage(i);
                if (page == DocumentPage.Missing) continue;

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    // خلفية بيضاء للصفحة.
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                    // محتوى الفاتورة.
                    var brush = new VisualBrush(page.Visual)
                    {
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };
                    dc.DrawRectangle(brush, null, new Rect(0, 0, width, height));

                    // نفس الإطار المستعمل عند الطباعة حتى يكون ملف PDF مطابقاً للورقة المطبوعة.
                    var outer = new Rect(13, 13, width - 26, height - 26);
                    var inner = new Rect(18, 18, width - 36, height - 36);
                    var pen = new Pen(new SolidColorBrush(Color.FromRgb(25, 58, 95)), 1.2);
                    var thinPen = new Pen(new SolidColorBrush(Color.FromRgb(175, 185, 195)), 0.45);
                    dc.DrawRectangle(null, pen, outer);
                    dc.DrawRectangle(null, thinPen, inner);
                }

                int pixelWidth = (int)Math.Round(width * RenderScale);
                int pixelHeight = (int)Math.Round(height * RenderScale);

                var bitmap = new RenderTargetBitmap(
                    pixelWidth, pixelHeight,
                    96 * RenderScale, 96 * RenderScale,
                    PixelFormats.Pbgra32);
                bitmap.Render(visual);
                bitmap.Freeze();

                var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var ms = new MemoryStream();
                encoder.Save(ms);

                result.Add(new RenderedPage
                {
                    JpegBytes = ms.ToArray(),
                    PixelWidth = pixelWidth,
                    PixelHeight = pixelHeight
                });
            }

            return result;
        }

        private static FlowDocument CloneDocument(FlowDocument source)
        {
            string xaml = XamlWriter.Save(source);
            using var stringReader = new StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (FlowDocument)XamlReader.Load(xmlReader);
        }

        // ============================================================
        //  2) كتابة ملف PDF قياسي (PDF 1.4) يدوياً
        // ============================================================

        private static void WritePdf(Stream output, List<RenderedPage> pages)
        {
            // ترقيم الكائنات:
            // 1 = الفهرس (Catalog)، 2 = قائمة الصفحات (Pages)
            // ثم لكل صفحة: كائن الصفحة + محتواها + صورتها
            int objectCount = 2 + pages.Count * 3;
            var offsets = new long[objectCount + 1];

            var buffer = new MemoryStream();
            WriteAscii(buffer, "%PDF-1.4\n");
            // تعليق ثنائي يخبر القارئات أن الملف يحتوي بيانات ثنائية.
            buffer.WriteByte(0x25);
            buffer.Write(new byte[] { 0xE2, 0xE3, 0xCF, 0xD3 }, 0, 4);
            buffer.WriteByte(0x0A);

            // --- 1: Catalog ---
            offsets[1] = buffer.Position;
            WriteAscii(buffer, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // --- 2: Pages ---
            offsets[2] = buffer.Position;
            var kids = new StringBuilder();
            for (int i = 0; i < pages.Count; i++)
            {
                kids.Append(3 + i * 3).Append(" 0 R ");
            }
            WriteAscii(buffer,
                $"2 0 obj\n<< /Type /Pages /Count {pages.Count} /Kids [ {kids.ToString().Trim()} ] >>\nendobj\n");

            // --- صفحات الفاتورة ---
            for (int i = 0; i < pages.Count; i++)
            {
                int pageObj = 3 + i * 3;
                int contentObj = pageObj + 1;
                int imageObj = pageObj + 2;
                var page = pages[i];

                string w = A4WidthPt.ToString("0.###", CultureInfo.InvariantCulture);
                string h = A4HeightPt.ToString("0.###", CultureInfo.InvariantCulture);

                // كائن الصفحة
                offsets[pageObj] = buffer.Position;
                WriteAscii(buffer,
                    $"{pageObj} 0 obj\n" +
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {w} {h}] " +
                    $"/Resources << /XObject << /Im0 {imageObj} 0 R >> >> " +
                    $"/Contents {contentObj} 0 R >>\nendobj\n");

                // محتوى الصفحة: وضع الصورة لتغطي الورقة كاملة
                string content = $"q\n{w} 0 0 {h} 0 0 cm\n/Im0 Do\nQ\n";
                byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                offsets[contentObj] = buffer.Position;
                WriteAscii(buffer, $"{contentObj} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
                buffer.Write(contentBytes, 0, contentBytes.Length);
                WriteAscii(buffer, "endstream\nendobj\n");

                // صورة الصفحة (JPEG مضغوطة داخل الملف)
                offsets[imageObj] = buffer.Position;
                WriteAscii(buffer,
                    $"{imageObj} 0 obj\n" +
                    $"<< /Type /XObject /Subtype /Image /Width {page.PixelWidth} /Height {page.PixelHeight} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode " +
                    $"/Length {page.JpegBytes.Length} >>\nstream\n");
                buffer.Write(page.JpegBytes, 0, page.JpegBytes.Length);
                WriteAscii(buffer, "\nendstream\nendobj\n");
            }

            // --- جدول المراجع (xref) ---
            long xrefPosition = buffer.Position;
            WriteAscii(buffer, $"xref\n0 {objectCount + 1}\n");
            WriteAscii(buffer, "0000000000 65535 f \n");
            for (int i = 1; i <= objectCount; i++)
            {
                WriteAscii(buffer, offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
            }

            WriteAscii(buffer,
                $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");

            buffer.Position = 0;
            buffer.CopyTo(output);
        }

        private static void WriteAscii(Stream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
