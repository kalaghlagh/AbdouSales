using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AbdouSales.Services
{
    /// <summary>
    /// قراءة وكتابة ملفات Excel (.xlsx) بدون أي مكتبة خارجية (ZIP + XML فقط).
    /// القراءة: أول ورقة في الملف، وكل الخلايا تُعاد كنصوص كما كُتبت (فلا يتغير ترتيب أو شكل القيم).
    /// الكتابة: تُستعمل لإنشاء نموذج جاهز للتعبئة.
    /// </summary>
    public static class ExcelHelper
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelDoc = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace RelPkg = "http://schemas.openxmlformats.org/package/2006/relationships";

        // ============================== القراءة ==============================

        /// <summary>يقرأ أول ورقة من ملف xlsx ويعيد الصفوف (كل صف مصفوفة نصوص بحسب فهرس العمود).</summary>
        public static List<string[]> ReadFirstSheet(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // يعمل حتى لو الملف مفتوح في Excel
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

            var shared = ReadSharedStrings(zip);
            string sheetPath = ResolveFirstSheetPath(zip);

            var entry = zip.GetEntry(sheetPath) ?? throw new InvalidDataException("تعذر العثور على ورقة البيانات داخل الملف.");
            XDocument doc;
            using (var s = entry.Open()) doc = XDocument.Load(s);

            var rows = new List<string[]>();
            foreach (var rowEl in doc.Descendants(Main + "row"))
            {
                var cells = new Dictionary<int, string>();
                int nextCol = 0;
                foreach (var c in rowEl.Elements(Main + "c"))
                {
                    int col = ColumnIndex((string?)c.Attribute("r"), nextCol);
                    nextCol = col + 1;
                    cells[col] = ReadCell(c, shared);
                }

                int rowIndex = (int?)rowEl.Attribute("r") ?? (rows.Count + 1);
                while (rows.Count < rowIndex - 1) rows.Add(Array.Empty<string>()); // الحفاظ على أرقام الصفوف الحقيقية

                var arr = new string[cells.Count == 0 ? 0 : cells.Keys.Max() + 1];
                for (int i = 0; i < arr.Length; i++) arr[i] = cells.TryGetValue(i, out var v) ? v : string.Empty;
                rows.Add(arr);
            }
            return rows;
        }

        private static string ResolveFirstSheetPath(ZipArchive zip)
        {
            var wbEntry = zip.GetEntry("xl/workbook.xml") ?? throw new InvalidDataException("الملف ليس ملف Excel بصيغة xlsx صالحة.");
            XDocument wb;
            using (var s = wbEntry.Open()) wb = XDocument.Load(s);

            string? rid = (string?)wb.Descendants(Main + "sheet").FirstOrDefault()?.Attribute(RelDoc + "id");
            var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (rid != null && relsEntry != null)
            {
                XDocument rels;
                using (var s = relsEntry.Open()) rels = XDocument.Load(s);
                string? target = rels.Descendants(RelPkg + "Relationship")
                    .FirstOrDefault(r => (string?)r.Attribute("Id") == rid)?.Attribute("Target")?.Value;
                if (!string.IsNullOrEmpty(target))
                    return target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
            }
            return "xl/worksheets/sheet1.xml";
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return list;

            XDocument doc;
            using (var s = entry.Open()) doc = XDocument.Load(s);
            foreach (var si in doc.Descendants(Main + "si"))
            {
                // نص عادي <t> أو نص منسق <r><t>؛ نتجاهل النطق الصوتي <rPh>
                var sb = new StringBuilder();
                foreach (var t in si.Descendants(Main + "t").Where(t => t.Parent?.Name != Main + "rPh"))
                    sb.Append(t.Value);
                list.Add(sb.ToString());
            }
            return list;
        }

        private static string ReadCell(XElement c, List<string> shared)
        {
            string type = (string?)c.Attribute("t") ?? "n";
            string raw = (string?)c.Element(Main + "v") ?? string.Empty;

            switch (type)
            {
                case "s":
                    return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) && idx >= 0 && idx < shared.Count
                        ? shared[idx].Trim() : string.Empty;
                case "inlineStr":
                    return string.Concat(c.Descendants(Main + "t").Select(t => t.Value)).Trim();
                case "b":
                    return raw == "1" ? "TRUE" : "FALSE";
                case "e":
                    return string.Empty;
                case "str":
                    return raw.Trim();
                default: // رقم: نعيده بشكل مقروء (دون صيغة علمية 1.2E+15)
                    return NormalizeNumber(raw);
            }
        }

        private static string NormalizeNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d.ToString("0.##########", CultureInfo.InvariantCulture);
            return raw.Trim();
        }

        private static int ColumnIndex(string? cellRef, int fallback)
        {
            if (string.IsNullOrEmpty(cellRef)) return fallback;
            int col = 0;
            foreach (char ch in cellRef)
            {
                if (!char.IsLetter(ch)) break;
                col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            return col == 0 ? fallback : col - 1;
        }

        // ============================== الكتابة (نموذج جاهز) ==============================

        /// <summary>ينشئ ملف xlsx بسيطاً: صف عناوين بخط عريض + صفوف أمثلة. كل القيم نصوص (حتى لا تضيع الأصفار في بداية الأرقام).</summary>
        public static void WriteSimpleWorkbook(string path, string[] headers, IEnumerable<string[]> sampleRows)
        {
            if (File.Exists(path)) File.Delete(path);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

            AddEntry(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>");

            AddEntry(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

            AddEntry(zip, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

            AddEntry(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>");

            // الأنماط: 0 عادي، 1 عناوين بخط عريض
            AddEntry(zip, "xl/styles.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs>" +
                "</styleSheet>");

            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false };
            var sheetEntry = zip.CreateEntry("xl/worksheets/sheet1.xml");
            using (var stream = sheetEntry.Open())
            using (var w = XmlWriter.Create(stream, settings))
            {
                w.WriteStartDocument(true);
                w.WriteStartElement("worksheet", Main.NamespaceName);
                w.WriteStartElement("sheetViews");
                w.WriteStartElement("sheetView");
                w.WriteAttributeString("rightToLeft", "1");
                w.WriteAttributeString("workbookViewId", "0");
                w.WriteEndElement(); w.WriteEndElement();

                w.WriteStartElement("cols");
                for (int i = 0; i < headers.Length; i++)
                {
                    w.WriteStartElement("col");
                    w.WriteAttributeString("min", (i + 1).ToString(CultureInfo.InvariantCulture));
                    w.WriteAttributeString("max", (i + 1).ToString(CultureInfo.InvariantCulture));
                    w.WriteAttributeString("width", "24");
                    w.WriteAttributeString("customWidth", "1");
                    w.WriteAttributeString("style", "2"); // تنسيق «نص»: يحفظ الأصفار والرموز عند الكتابة في العمود
                    w.WriteEndElement();
                }
                w.WriteEndElement();

                w.WriteStartElement("sheetData");
                int rowNo = 1;
                WriteRow(w, rowNo++, headers, 1);
                foreach (var r in sampleRows) WriteRow(w, rowNo++, r, 2);
                w.WriteEndElement(); // sheetData
                w.WriteEndElement(); // worksheet
                w.WriteEndDocument();
            }
        }

        private static void WriteRow(XmlWriter w, int rowNo, string[] values, int style)
        {
            w.WriteStartElement("row");
            w.WriteAttributeString("r", rowNo.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < values.Length; i++)
            {
                w.WriteStartElement("c");
                w.WriteAttributeString("r", ColumnName(i) + rowNo.ToString(CultureInfo.InvariantCulture));
                w.WriteAttributeString("t", "inlineStr");
                if (style != 0) w.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
                w.WriteStartElement("is");
                w.WriteStartElement("t");
                w.WriteAttributeString("xml", "space", null, "preserve");
                w.WriteString(values[i] ?? string.Empty);
                w.WriteEndElement(); w.WriteEndElement(); w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        private static string ColumnName(int index)
        {
            var sb = new StringBuilder();
            for (int n = index + 1; n > 0; n = (n - 1) / 26) sb.Insert(0, (char)('A' + (n - 1) % 26));
            return sb.ToString();
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }
    }
}
