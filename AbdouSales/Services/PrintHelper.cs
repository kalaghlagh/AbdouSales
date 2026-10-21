using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace AbdouSales.Services
{
    public static class PrintHelper
    {
        public static bool Print(FlowDocument document, string jobName = "فاتورة")
        {
            var printDialog = new PrintDialog();
            try
            {
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            }
            catch { }

            if (printDialog.ShowDialog() != true) return false;

            try
            {
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            }
            catch { }

            var copy = CloneDocument(document);
            double printableWidth = printDialog.PrintableAreaWidth > 0
                ? printDialog.PrintableAreaWidth
                : InvoiceDocumentBuilder.A4WidthPx;
            double printableHeight = printDialog.PrintableAreaHeight > 0
                ? printDialog.PrintableAreaHeight
                : InvoiceDocumentBuilder.A4HeightPx;

            copy.PageWidth = printableWidth;
            copy.PageHeight = printableHeight;
            copy.PagePadding = new Thickness(InvoiceDocumentBuilder.PageMarginPx);

            IDocumentPaginatorSource paginatorSource = copy;
            var paginator = new A4BorderPaginator(
                paginatorSource.DocumentPaginator,
                printableWidth,
                printableHeight);

            printDialog.PrintDocument(paginator, jobName);
            return true;
        }

        private static FlowDocument CloneDocument(FlowDocument source)
        {
            var xaml = XamlWriter.Save(source);
            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (FlowDocument)XamlReader.Load(xmlReader);
        }

        private sealed class A4BorderPaginator : DocumentPaginator
        {
            private readonly DocumentPaginator _inner;
            private readonly Size _pageSize;

            public A4BorderPaginator(DocumentPaginator inner, double width, double height)
            {
                _inner = inner;
                _pageSize = new Size(width, height);
            }

            public override DocumentPage GetPage(int pageNumber)
            {
                var sourcePage = _inner.GetPage(pageNumber);
                if (sourcePage == DocumentPage.Missing) return sourcePage;

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    // رسم محتوى الصفحة الأصلي.
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, _pageSize.Width, _pageSize.Height));
                    var brush = new VisualBrush(sourcePage.Visual)
                    {
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };
                    dc.DrawRectangle(brush, null, new Rect(0, 0, _pageSize.Width, _pageSize.Height));

                    // إطار خارجي + خط داخلي رفيع.
                    var outer = new Rect(13, 13, _pageSize.Width - 26, _pageSize.Height - 26);
                    var inner = new Rect(18, 18, _pageSize.Width - 36, _pageSize.Height - 36);
                    var pen = new Pen(new SolidColorBrush(Color.FromRgb(25, 58, 95)), 1.2);
                    var thinPen = new Pen(new SolidColorBrush(Color.FromRgb(175, 185, 195)), 0.45);
                    dc.DrawRectangle(null, pen, outer);
                    dc.DrawRectangle(null, thinPen, inner);
                }

                return new DocumentPage(visual, _pageSize, sourcePage.BleedBox, sourcePage.ContentBox);
            }

            public override bool IsPageCountValid => _inner.IsPageCountValid;
            public override int PageCount => _inner.PageCount;
            public override Size PageSize { get => _pageSize; set => _inner.PageSize = value; }
            public override IDocumentPaginatorSource Source => _inner.Source;
        }
    }
}
