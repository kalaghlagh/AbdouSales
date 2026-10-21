using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AbdouSales.Models;

namespace AbdouSales.Services
{
    /// <summary>
    /// يبني فاتورة تجارية احترافية مناسبة للطباعة على A4، بأحد ثلاثة قوالب جاهزة
    /// (كلاسيكي، احترافي كحلي، عصري بالأزرق الملكي) يختارها المستخدم من صفحة "قوالب الفاتورة".
    /// التصميم: شعار ومعلومات الممون، بيانات الفاتورة، بيانات الزبون،
    /// جدول السلع، المجاميع، طريقة الدفع، المبلغ بالحروف، التوقيع والختم.
    /// </summary>
    public static class InvoiceDocumentBuilder
    {
        public const double A4WidthPx = 793.92;
        public const double A4HeightPx = 1122.72;
        public const double PageMarginPx = 42;

        private static readonly Brush SoftGray = new SolidColorBrush(Color.FromRgb(247, 248, 250));
        /// <summary>عدد صفوف جدول السلع الثابت: يُحجز دائماً مكان 10 سلع حتى لو أدخل المستخدم سلعة واحدة، لتبقى الفاتورة بمقاس A4.</summary>
        public const int ItemsTableRows = 10;

        /// <summary>ارتفاع سطر ثابت لكل صف في جدول السلع حتى تتساوى الصفوف الفارغة والممتلئة.</summary>
        private const double ItemRowLineHeight = 18;

        /// <summary>لون باهت لحدود جدول السلع (يشمل الصفوف الفارغة).</summary>
        private static readonly Brush FaintBorder = new SolidColorBrush(Color.FromRgb(222, 226, 232));

        private static readonly Brush BorderGray = new SolidColorBrush(Color.FromRgb(120, 130, 140));

        /// <summary>مجموعة الألوان/الخصائص المميزة لكل قالب.</summary>
        private sealed class Theme
        {
            public required Brush Accent;        // اللون الأساسي (عناوين، حدود، شعار)
            public required Brush AccentLight;    // خلفية فاتحة للتمييز (صف المجموع الكلي، خلفية بيانات الفاتورة)
            public required Brush HeaderText;     // لون نص أشرطة العناوين الملونة
            public bool HeaderFilled;             // هل تُملأ أشرطة العناوين باللون أم حدود فقط
            public bool Rounded;                  // زوايا دائرية للعناصر البصرية (شعار/ختم/بطاقات)
            public Brush TableBorder = BorderGray;
            public double BorderThickness = 0.6;
        }

        private static Theme GetTheme(InvoiceTemplateStyle style) => style switch
        {
            InvoiceTemplateStyle.ClassicClean => new Theme
            {
                Accent = Brushes.Black,
                AccentLight = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                HeaderText = Brushes.White,
                HeaderFilled = true,
                Rounded = false,
                TableBorder = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = 0.7
            },
            InvoiceTemplateStyle.ModernRoyalBlue => new Theme
            {
                Accent = new SolidColorBrush(Color.FromRgb(31, 86, 224)),
                AccentLight = new SolidColorBrush(Color.FromRgb(228, 236, 253)),
                HeaderText = Brushes.White,
                HeaderFilled = true,
                Rounded = true,
                TableBorder = new SolidColorBrush(Color.FromRgb(31, 86, 224)),
                BorderThickness = 0.9
            },
            _ => new Theme // ExecutiveNavy (الافتراضي/الأصلي)
            {
                Accent = new SolidColorBrush(Color.FromRgb(25, 58, 95)),
                AccentLight = new SolidColorBrush(Color.FromRgb(235, 242, 249)),
                HeaderText = Brushes.White,
                HeaderFilled = true,
                Rounded = false,
                TableBorder = BorderGray,
                BorderThickness = 0.6
            }
        };

        /// <summary>يبني الفاتورة باستعمال القالب المحدَّد صراحة.</summary>
        public static FlowDocument Build(Invoice invoice, CompanyInfo company, InvoiceTemplateStyle style)
        {
            var theme = GetTheme(style);

            var doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Arial"),
                FontSize = 14,
                PagePadding = new Thickness(PageMarginPx),
                ColumnWidth = double.PositiveInfinity,
                PageWidth = A4WidthPx,
                PageHeight = A4HeightPx,
                Background = Brushes.White
            };

            doc.Blocks.Add(BuildProfessionalHeader(invoice, company, theme));
            doc.Blocks.Add(BuildCustomerTable(invoice, theme));
            doc.Blocks.Add(BuildItemsTable(invoice, theme));
            doc.Blocks.Add(BuildTotalsAndPayment(invoice, theme));

            var wordsBox = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 7) };
            wordsBox.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var wordsGroup = new TableRowGroup();
            var wordsRow = new TableRow();
            var words = new Paragraph { Margin = new Thickness(0) };
            words.Inlines.Add(new Bold(new Run("المبلغ بالحروف: ")));
            words.Inlines.Add(new Run(NumberToArabicWords.Convert(invoice.GrandTotal)));
            wordsRow.Cells.Add(new TableCell(words)
            {
                Padding = new Thickness(8),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(theme.BorderThickness),
                Background = SoftGray
            });
            wordsGroup.Rows.Add(wordsRow);
            wordsBox.RowGroups.Add(wordsGroup);
            doc.Blocks.Add(wordsBox);

            doc.Blocks.Add(BuildSignatureBlock(theme));

            var footer = new Paragraph(new Run("هذه الفاتورة محررة آلياً وتعتبر وثيقة تجارية عند استكمال التوقيع والختم.")
            {
                FontSize = 8.5,
                Foreground = Brushes.Gray
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            doc.Blocks.Add(footer);

            return doc;
        }

        /// <summary>
        /// يبني الفاتورة باستعمال قالب الفاتورة نفسها إن كان محدَّداً، وإلا القالب الافتراضي للشركة.
        /// (يبقى هذا التوقيع القديم يعمل كما هو حتى لا تنكسر بقية الشاشات).
        /// </summary>
        public static FlowDocument Build(Invoice invoice, CompanyInfo company)
            => Build(invoice, company, invoice.TemplateStyle ?? company.DefaultTemplate);

        private static Block BuildProfessionalHeader(Invoice invoice, CompanyInfo company, Theme theme)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };

            table.Columns.Add(new TableColumn { Width = new GridLength(1.05, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(2.75, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.35, GridUnitType.Star) });

            var group = new TableRowGroup();
            var row = new TableRow();

            // الشعار.
            var logoPanel = new Grid { Width = 60, Height = 60 };
            var logoCircle = new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(theme.Rounded ? 30 : 34),
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(2.2),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var logoText = new TextBlock
            {
                Text = "A",
                FontFamily = new FontFamily("Arial"),
                FontSize = 34,
                FontWeight = FontWeights.Bold,
                Foreground = theme.Accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            logoCircle.Child = logoText;
            logoPanel.Children.Add(logoCircle);

            row.Cells.Add(new TableCell(new BlockUIContainer(logoPanel))
            {
                Padding = new Thickness(4),
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(1.1),
                TextAlignment = TextAlignment.Center
            });

            // معلومات الممون: إن اختار المستخدم مموّناً محدداً لهذه الفاتورة، تظهر بياناته في الترويسة
            // بدل بيانات الشركة الافتراضية من الإعدادات.
            bool useSupplier = !string.IsNullOrWhiteSpace(invoice.SupplierName);
            string headerName = useSupplier ? invoice.SupplierName : company.Name;
            string headerActivity = useSupplier ? invoice.SupplierActivity : company.Activity;
            string headerAddress = useSupplier ? invoice.SupplierAddress : company.Address;
            string headerCommercialRegister = useSupplier ? invoice.SupplierCommercialRegister : company.CommercialRegister;
            string headerTaxId = useSupplier ? invoice.SupplierTaxId : company.TaxId;
            string headerArticleNumber = useSupplier ? invoice.SupplierArticleNumber : company.ArticleNumber;

            var companySection = new Section();
            var companyName = string.IsNullOrWhiteSpace(headerName) ? "اسم الشركة / الممون" : headerName;
            companySection.Blocks.Add(new Paragraph(new Bold(new Run(companyName)))
            {
                FontSize = 28,
                Foreground = theme.Accent,
                Margin = new Thickness(0, 0, 0, 4),
                TextAlignment = TextAlignment.Center
            });
            AddCompanyLine(companySection, "النشاط", headerActivity);
            AddCompanyLine(companySection, "العنوان", headerAddress);
            AddCompanyLine(companySection, "السجل التجاري", headerCommercialRegister, keepOrder: true);
            AddCompanyLine(companySection, "الرقم الجبائي", headerTaxId, keepOrder: true);
            AddCompanyLine(companySection, "رقم المادة", headerArticleNumber, keepOrder: true);

            row.Cells.Add(new TableCell(companySection)
            {
                FontSize = 18,
                Padding = new Thickness(9, 5, 9, 5),
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(1.1)
            });

            // عنوان الفاتورة والبيانات.
            var meta = new Section();
            var title = new Paragraph(new Bold(new Run("فاتورة")))
            {
                FontSize = 21,
                Foreground = theme.Accent,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            meta.Blocks.Add(title);
            meta.Blocks.Add(MetaParagraph("رقم الفاتورة", invoice.Number));
            meta.Blocks.Add(MetaParagraph("التاريخ", invoice.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));

            row.Cells.Add(new TableCell(meta)
            {
                Padding = new Thickness(8),
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(1.1),
                Background = theme.AccentLight
            });

            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }

        private static void AddCompanyLine(Section section, string label, string value, bool keepOrder = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 1.5), FontSize = 14 };
            p.Inlines.Add(new Bold(new Run(label + ": ")));
            // القيم الرمزية (سجل تجاري، رقم جبائي...) تُعرض بترتيبها الأصلي مهما اختلط فيها العربي والأرقام والرموز
            if (keepOrder) p.Inlines.Add(BidiHelper.LtrInline(value, 14));
            else p.Inlines.Add(new Run(value));
            section.Blocks.Add(p);
        }

        private static Paragraph MetaParagraph(string label, string value)
        {
            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 5), TextAlignment = TextAlignment.Center };
            p.Inlines.Add(new Bold(new Run(label + "\n")));
            p.Inlines.Add(new Run(value ?? ""));
            return p;
        }

        private static Block BuildCustomerTable(Invoice invoice, Theme theme)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 9) };
            table.Columns.Add(new TableColumn { Width = new GridLength(1.15, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.85, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.15, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.85, GridUnitType.Star) });

            var group = new TableRowGroup();
            var titleRow = new TableRow();
            titleRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("معلومات الزبون"))))
            {
                ColumnSpan = 4,
                Background = theme.HeaderFilled ? theme.Accent : Brushes.White,
                Foreground = theme.HeaderFilled ? theme.HeaderText : theme.Accent,
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(0.7),
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(6)
            });
            group.Rows.Add(titleRow);

            AddCustomerRow(group, "اسم الزبون", invoice.CustomerName, "النشاط", invoice.CustomerActivity, theme);
            AddCustomerRow(group, "العنوان", invoice.CustomerAddress, "السجل التجاري", invoice.CustomerCommercialRegister, theme, keepOrder1: false, keepOrder2: true);
            AddCustomerRow(group, "الرقم الجبائي", invoice.CustomerTaxId, "طريقة الدفع", invoice.PaymentMethod, theme, keepOrder1: true);

            table.RowGroups.Add(group);
            return table;
        }

        private static void AddCustomerRow(TableRowGroup group, string label1, string value1, string label2, string value2, Theme theme,
            bool keepOrder1 = false, bool keepOrder2 = false)
        {
            var row = new TableRow();
            row.Cells.Add(LabelCell(label1, theme));
            row.Cells.Add(ValueCell(value1, theme, keepOrder1));
            row.Cells.Add(LabelCell(label2, theme));
            row.Cells.Add(ValueCell(value2, theme, keepOrder2));
            group.Rows.Add(row);
        }

        private static TableCell LabelCell(string text, Theme theme) => new(new Paragraph(new Bold(new Run(text ?? "")))
        {
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(0)
        })
        {
            Background = SoftGray,
            BorderBrush = theme.TableBorder,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(5)
        };

        // keepOrder: القيم الرمزية المختلطة (سجل تجاري، رقم جبائي) تُعرض في عنصر اتجاهه من اليسار إلى اليمين فيبقى ترتيبها كما كُتبت
        private static TableCell ValueCell(string text, Theme theme, bool keepOrder = false)
        {
            var paragraph = keepOrder
                ? new Paragraph(BidiHelper.LtrInline(text, 14))
                : new Paragraph(new Run(text ?? ""));
            paragraph.TextAlignment = TextAlignment.Left;
            paragraph.Margin = new Thickness(0);
            paragraph.FlowDirection = FlowDirection.RightToLeft;
            return new TableCell(paragraph)
            {
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(5)
            };
        }

        private static Block BuildItemsTable(Invoice invoice, Theme theme)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 9) };

            double[] widths = { 0.55, 3.05, 0.85, 0.85, 1.45, 1.55 };
            foreach (var width in widths)
                table.Columns.Add(new TableColumn { Width = new GridLength(width, GridUnitType.Star) });

            var headerGroup = new TableRowGroup();
            var header = new TableRow { Background = theme.Accent, FontWeight = FontWeights.Bold };
            string[] headers = { "رقم", "التعيين", "الوحدة", "الكمية", "سعر الوحدة (د.ج)", "المجموع (د.ج)" };
            foreach (var text in headers) header.Cells.Add(HeaderCell(text, theme));
            headerGroup.Rows.Add(header);
            table.RowGroups.Add(headerGroup);

            var body = new TableRowGroup();
            int index = 1;
            foreach (var line in invoice.Lines)
            {
                var row = new TableRow();
                row.Cells.Add(BodyCell(index.ToString(CultureInfo.InvariantCulture), theme));
                row.Cells.Add(BodyCell(line.ProductName, theme, TextAlignment.Center));
                row.Cells.Add(BodyCell(line.Unit, theme));
                row.Cells.Add(BodyCell(line.Quantity.ToString("0.##", CultureInfo.InvariantCulture), theme));
                row.Cells.Add(BodyCell(line.UnitPrice.ToString("N2", CultureInfo.InvariantCulture), theme));
                row.Cells.Add(BodyCell(line.Total.ToString("N2", CultureInfo.InvariantCulture), theme));
                body.Rows.Add(row);
                index++;
            }

            // نكمل بصفوف فارغة حتى يبلغ الجدول 10 صفوف دائماً؛ وإن تجاوزت السلع 10 يتّسع الجدول لها.
            while (body.Rows.Count < ItemsTableRows)
            {
                var empty = new TableRow();
                for (int i = 0; i < 6; i++) empty.Cells.Add(BodyCell("\u00A0", theme));
                body.Rows.Add(empty);
            }

            table.RowGroups.Add(body);
            return table;
        }

        private static TableCell HeaderCell(string text, Theme theme) => new(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        })
        {
            Padding = new Thickness(4, 6, 4, 6),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(0.35),
            Foreground = theme.HeaderText
        };

        private static TableCell BodyCell(string text, Theme theme, TextAlignment alignment = TextAlignment.Center) => new(new Paragraph(new Run(text ?? ""))
        {
            TextAlignment = alignment,
            Margin = new Thickness(0),
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            LineHeight = ItemRowLineHeight
        })
        {
            Padding = new Thickness(4, 3, 4, 3),
            BorderBrush = FaintBorder,
            BorderThickness = new Thickness(0.5)
        };

        private static Block BuildTotalsAndPayment(Invoice invoice, Theme theme)
        {
            var outer = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 6) };
            outer.Columns.Add(new TableColumn { Width = new GridLength(1.8, GridUnitType.Star) });
            outer.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var group = new TableRowGroup();
            var row = new TableRow();

            var payment = new Section();
            payment.Blocks.Add(new Paragraph(new Bold(new Run("طريقة الدفع")))
            {
                FontSize = 12,
                Foreground = theme.Accent,
                Margin = new Thickness(0, 0, 0, 8)
            });
            payment.Blocks.Add(new Paragraph(new Run(invoice.PaymentMethod ?? ""))
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 7)
            });
            payment.Blocks.Add(new Paragraph(new Run("شكراً لتعاملكم معنا"))
            {
                FontSize = 9.5,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0)
            });

            row.Cells.Add(new TableCell(payment)
            {
                Padding = new Thickness(9),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.7)
            });

            var totals = new Table { CellSpacing = 0 };
            totals.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
            totals.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var tg = new TableRowGroup();
            AddTotalRow(tg, "المجموع بدون رسوم", $"{invoice.SubTotal:N2} د.ج", theme);
            AddTotalRow(tg, $"الرسوم (TVA {invoice.VatRate:0.##}%)", $"{invoice.VatAmount:N2} د.ج", theme);
            if (invoice.FiscalStamp > 0) AddTotalRow(tg, "الطابع الجبائي", $"{invoice.FiscalStamp:N2} د.ج", theme);
            AddTotalRow(tg, "المجموع الكلي", $"{invoice.GrandTotal:N2} د.ج", theme, true);
            if (invoice.IsCashierSale && invoice.AmountReceived > 0)
            {
                AddTotalRow(tg, "المبلغ المستلم", $"{invoice.AmountReceived:N2} د.ج", theme);
                AddTotalRow(tg, "الباقي", $"{invoice.ChangeGiven:N2} د.ج", theme);
            }
            totals.RowGroups.Add(tg);

            row.Cells.Add(new TableCell(totals)
            {
                Padding = new Thickness(0),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.7)
            });

            group.Rows.Add(row);
            outer.RowGroups.Add(group);
            return outer;
        }

        private static void AddTotalRow(TableRowGroup group, string label, string value, Theme theme, bool emphasize = false)
        {
            var bg = emphasize ? theme.AccentLight : Brushes.White;
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(label))) { Margin = new Thickness(0) })
            {
                Padding = new Thickness(6),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.5),
                Background = bg
            });
            row.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(value)))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            })
            {
                Padding = new Thickness(6),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.5),
                Background = bg
            });
            group.Rows.Add(row);
        }

        private static Block BuildSignatureBlock(Theme theme)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 0) };
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var group = new TableRowGroup();
            var row = new TableRow();

           
            const int V = 45;
            var supplierPanel = new Grid { Width = 230, Height = V };
            supplierPanel.Children.Add(new TextBlock
            {
                Text = "الامضاء / الختم",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0)
            });

            var stamp = new Border
            {
                Width = 88,
                Height = 45,
                CornerRadius = new CornerRadius(theme.Rounded ? 10 : 0),
                BorderBrush = theme.Accent,
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 1)
            };
           

            row.Cells.Add(new TableCell(new BlockUIContainer(supplierPanel))
            {
                Padding = new Thickness(9),
                BorderBrush = theme.TableBorder,
                BorderThickness = new Thickness(0.7),
                LineHeight = V
            });

            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }
    }
}
