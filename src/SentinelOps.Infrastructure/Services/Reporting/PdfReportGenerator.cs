using System.Globalization;
using System.Text;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Reporting;

public class PdfReportGenerator
{
    private const double PageWidth = 612.0;   // US Letter Width
    private const double PageHeight = 792.0;  // US Letter Height
    private const double MarginLeft = 36.0;
    private const double MarginRight = 36.0;
    private const double PrintableWidth = PageWidth - MarginLeft - MarginRight; // 540.0 pt

    public static byte[] Generate(ReportPreviewDto report, string requestedByUser = "Security Auditor")
    {
        var pages = new List<StringBuilder>();
        var currentPage = new StringBuilder();
        pages.Add(currentPage);

        double y = PageHeight - 40; // Start near top

        // Draw First Page Header
        y = DrawHeader(currentPage, report, requestedByUser, y);

        // Draw KPI Summary Cards (if any)
        if (report.SummaryMetrics != null && report.SummaryMetrics.Count > 0)
        {
            y = DrawKpiCards(currentPage, report.SummaryMetrics, y);
        }

        // Draw Table Section
        if (report.ColumnHeaders != null && report.ColumnHeaders.Count > 0)
        {
            y -= 10;
            DrawText(currentPage, "DETAILED TELEMETRY & AUDIT RECORDS", MarginLeft, y, 10, true, "0.12 0.16 0.23");
            y -= 16;

            int colCount = report.ColumnHeaders.Count;
            double colWidth = PrintableWidth / colCount;

            // Table Header
            DrawTableHeader(currentPage, report.ColumnHeaders, colWidth, y);
            y -= 20;

            int rowIndex = 0;
            foreach (var row in report.Rows)
            {
                // Check if row fits on current page (leave room for footer at bottom)
                if (y < 65)
                {
                    // Create new page
                    currentPage = new StringBuilder();
                    pages.Add(currentPage);
                    y = PageHeight - 40;

                    // Draw mini header on subsequent pages
                    y = DrawSubsequentHeader(currentPage, report, y);
                    DrawTableHeader(currentPage, report.ColumnHeaders, colWidth, y);
                    y -= 20;
                }

                DrawTableRow(currentPage, row, colWidth, y, rowIndex % 2 == 1);
                y -= 17;
                rowIndex++;
            }
        }

        // Finalize all pages with footers and page numbers (Two-pass)
        int totalPages = pages.Count;
        for (int i = 0; i < totalPages; i++)
        {
            DrawFooter(pages[i], i + 1, totalPages);
        }

        return CompilePdf(pages);
    }

    private static double DrawHeader(StringBuilder sb, ReportPreviewDto report, string user, double y)
    {
        // Brand accent top bar
        DrawRect(sb, 0, PageHeight - 8, PageWidth, 8, "0.01 0.52 0.78");

        // SentinelOps Logo / Header text
        DrawText(sb, "SENTINELOPS", MarginLeft, y, 16, true, "0.01 0.52 0.78");
        DrawText(sb, "ENTERPRISE OBSERVABILITY PLATFORM", MarginLeft + 120, y + 1, 9, false, "0.39 0.45 0.55");
        y -= 24;

        // Title
        DrawText(sb, CleanText(report.Title), MarginLeft, y, 16, true, "0.06 0.09 0.16");
        y -= 14;

        // Description
        if (!string.IsNullOrWhiteSpace(report.Description))
        {
            DrawText(sb, CleanText(report.Description), MarginLeft, y, 9, false, "0.39 0.45 0.55");
            y -= 14;
        }

        // Metadata Bar
        y -= 6;
        DrawRect(sb, MarginLeft, y - 24, PrintableWidth, 24, "0.96 0.97 0.99", "0.85 0.88 0.92");
        string metaText = $"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC   |   Period: {report.StartUtc:yyyy-MM-dd HH:mm} to {report.EndUtc:yyyy-MM-dd HH:mm} UTC   |   User: {CleanText(user)}";
        DrawText(sb, metaText, MarginLeft + 10, y - 16, 7.5, false, "0.25 0.30 0.38");
        y -= 36;

        return y;
    }

    private static double DrawSubsequentHeader(StringBuilder sb, ReportPreviewDto report, double y)
    {
        DrawRect(sb, 0, PageHeight - 6, PageWidth, 6, "0.01 0.52 0.78");
        DrawText(sb, $"SENTINELOPS — {CleanText(report.Title).ToUpperInvariant()} (CONTINUED)", MarginLeft, y, 9, true, "0.39 0.45 0.55");
        y -= 20;
        return y;
    }

    private static double DrawKpiCards(StringBuilder sb, Dictionary<string, object> metrics, double y)
    {
        var items = metrics.Take(4).ToList();
        if (items.Count == 0) return y;

        double gap = 12.0;
        double cardWidth = (PrintableWidth - (gap * (items.Count - 1))) / items.Count;
        double cardHeight = 44.0;

        for (int i = 0; i < items.Count; i++)
        {
            double cx = MarginLeft + (i * (cardWidth + gap));
            double cy = y - cardHeight;

            // Card background & border
            DrawRect(sb, cx, cy, cardWidth, cardHeight, "0.97 0.98 0.99", "0.85 0.88 0.92");

            // Metric Label
            DrawText(sb, CleanText(items[i].Key).ToUpperInvariant(), cx + 8, cy + 28, 7.5, true, "0.39 0.45 0.55");

            // Metric Value
            string val = CleanText(items[i].Value?.ToString() ?? "0");
            DrawText(sb, val, cx + 8, cy + 10, 13, true, "0.01 0.52 0.78");
        }

        return y - cardHeight - 16;
    }

    private static void DrawTableHeader(StringBuilder sb, List<string> headers, double colWidth, double y)
    {
        // Dark header row background
        DrawRect(sb, MarginLeft, y, PrintableWidth, 20, "0.09 0.13 0.20");

        for (int i = 0; i < headers.Count; i++)
        {
            double hx = MarginLeft + (i * colWidth) + 6;
            string title = Truncate(CleanText(headers[i]).ToUpperInvariant(), (int)(colWidth / 5.5));
            DrawText(sb, title, hx, y + 6, 8, true, "1.0 1.0 1.0");
        }
    }

    private static void DrawTableRow(StringBuilder sb, List<string> row, double colWidth, double y, bool isZebra)
    {
        // Background fill
        string fill = isZebra ? "0.96 0.97 0.99" : "1.0 1.0 1.0";
        DrawRect(sb, MarginLeft, y, PrintableWidth, 17, fill, "0.90 0.92 0.95");

        for (int i = 0; i < row.Count; i++)
        {
            double rx = MarginLeft + (i * colWidth) + 6;
            string cell = Truncate(CleanText(row[i]), (int)(colWidth / 5.2));
            DrawText(sb, cell, rx, y + 4.5, 7.5, false, "0.15 0.18 0.25");
        }
    }

    private static void DrawFooter(StringBuilder sb, int pageNum, int totalPages)
    {
        double fy = 35.0;
        DrawLine(sb, MarginLeft, fy, MarginLeft + PrintableWidth, fy, "0.85 0.88 0.92", 0.5);

        DrawText(sb, "CONFIDENTIAL — PROPRIETARY OBSERVABILITY & SECURITY TELEMETRY", MarginLeft, 22, 7.0, false, "0.50 0.55 0.65");
        string pageStr = $"Page {pageNum} of {totalPages}";
        DrawText(sb, pageStr, MarginLeft + PrintableWidth - 60, 22, 7.5, true, "0.30 0.35 0.45");
    }

    // PDF Vector Drawing Helpers

    private static void DrawText(StringBuilder sb, string text, double x, double y, double fontSize, bool isBold, string rgb)
    {
        string font = isBold ? "/F2" : "/F1";
        sb.AppendLine("BT");
        sb.AppendLine($"{rgb} rg");
        sb.AppendLine($"{font} {fontSize.ToString("F1", CultureInfo.InvariantCulture)} Tf");
        sb.AppendLine($"1 0 0 1 {x.ToString("F2", CultureInfo.InvariantCulture)} {y.ToString("F2", CultureInfo.InvariantCulture)} Tm");
        sb.AppendLine($"({EscapePdf(text)}) Tj");
        sb.AppendLine("ET");
    }

    private static void DrawRect(StringBuilder sb, double x, double y, double w, double h, string fillRgb, string? strokeRgb = null, double lineWidth = 0.5)
    {
        sb.AppendLine("q");
        sb.AppendLine($"{fillRgb} rg");
        if (strokeRgb != null)
        {
            sb.AppendLine($"{strokeRgb} RG");
            sb.AppendLine($"{lineWidth.ToString("F1", CultureInfo.InvariantCulture)} w");
            sb.AppendLine($"{x.ToString("F2", CultureInfo.InvariantCulture)} {y.ToString("F2", CultureInfo.InvariantCulture)} {w.ToString("F2", CultureInfo.InvariantCulture)} {h.ToString("F2", CultureInfo.InvariantCulture)} re B");
        }
        else
        {
            sb.AppendLine($"{x.ToString("F2", CultureInfo.InvariantCulture)} {y.ToString("F2", CultureInfo.InvariantCulture)} {w.ToString("F2", CultureInfo.InvariantCulture)} {h.ToString("F2", CultureInfo.InvariantCulture)} re f");
        }
        sb.AppendLine("Q");
    }

    private static void DrawLine(StringBuilder sb, double x1, double y1, double x2, double y2, string strokeRgb, double lineWidth = 0.5)
    {
        sb.AppendLine("q");
        sb.AppendLine($"{strokeRgb} RG");
        sb.AppendLine($"{lineWidth.ToString("F1", CultureInfo.InvariantCulture)} w");
        sb.AppendLine($"{x1.ToString("F2", CultureInfo.InvariantCulture)} {y1.ToString("F2", CultureInfo.InvariantCulture)} m");
        sb.AppendLine($"{x2.ToString("F2", CultureInfo.InvariantCulture)} {y2.ToString("F2", CultureInfo.InvariantCulture)} l S");
        sb.AppendLine("Q");
    }

    private static string EscapePdf(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c == '\\') sb.Append("\\\\");
            else if (c == '(') sb.Append("\\(");
            else if (c == ')') sb.Append("\\)");
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private static string CleanText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            // Replace non-ASCII / special characters with safe ASCII equivalents
            switch (c)
            {
                case 'ı': sb.Append('i'); break;
                case 'İ': sb.Append('I'); break;
                case 'ş': sb.Append('s'); break;
                case 'Ş': sb.Append('S'); break;
                case 'ğ': sb.Append('g'); break;
                case 'Ğ': sb.Append('G'); break;
                case 'ü': sb.Append('u'); break;
                case 'Ü': sb.Append('U'); break;
                case 'ö': sb.Append('o'); break;
                case 'Ö': sb.Append('O'); break;
                case 'ç': sb.Append('c'); break;
                case 'Ç': sb.Append('C'); break;
                case '—':
                case '–': sb.Append('-'); break;
                case '“':
                case '”': sb.Append('"'); break;
                case '‘':
                case '’': sb.Append('\''); break;
                default:
                    if (c >= 32 && c <= 126) sb.Append(c);
                    else if (c == '\t') sb.Append("  ");
                    else sb.Append(' ');
                    break;
            }
        }
        return sb.ToString().Trim();
    }

    private static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text;
        if (maxLen <= 3) return text[..maxLen];
        return text[..(maxLen - 3)] + "...";
    }

    private static byte[] CompilePdf(List<StringBuilder> pageStreams)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII);

        var offsets = new List<long>();

        // 1. Header
        writer.Write("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        writer.Flush();

        int totalPages = pageStreams.Count;
        int objectCount = 4 + (totalPages * 2); // 1:Catalog, 2:Pages, 3:F1, 4:F2, then Page + Content per page

        // Object 1: Catalog
        offsets.Add(ms.Position);
        writer.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        writer.Flush();

        // Object 2: Pages
        offsets.Add(ms.Position);
        var kids = new StringBuilder();
        for (int i = 0; i < totalPages; i++)
        {
            int pageObjNum = 5 + (i * 2);
            kids.Append($"{pageObjNum} 0 R ");
        }
        writer.Write($"2 0 obj\n<< /Type /Pages /Kids [ {kids} ] /Count {totalPages} >>\nendobj\n");
        writer.Flush();

        // Object 3: Font Helvetica Regular
        offsets.Add(ms.Position);
        writer.Write("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // Object 4: Font Helvetica Bold
        offsets.Add(ms.Position);
        writer.Write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // Write Pages & Contents
        for (int i = 0; i < totalPages; i++)
        {
            int pageObjNum = 5 + (i * 2);
            int contentObjNum = 6 + (i * 2);

            // Page Object
            offsets.Add(ms.Position);
            writer.Write($"{pageObjNum} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 {PageWidth.ToString("F1", CultureInfo.InvariantCulture)} {PageHeight.ToString("F1", CultureInfo.InvariantCulture)} ] /Contents {contentObjNum} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>\nendobj\n");
            writer.Flush();

            // Content Stream Object
            byte[] streamBytes = Encoding.ASCII.GetBytes(pageStreams[i].ToString());
            offsets.Add(ms.Position);
            writer.Write($"{contentObjNum} 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
            writer.Flush();
            ms.Write(streamBytes, 0, streamBytes.Length);
            writer.Write("\nendstream\nendobj\n");
            writer.Flush();
        }

        // XRef Table
        long xrefPos = ms.Position;
        writer.Write($"xref\n0 {objectCount + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            writer.Write($"{offset:D10} 00000 n \n");
        }

        // Trailer
        writer.Write($"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\n");
        writer.Write($"startxref\n{xrefPos}\n%%EOF\n");
        writer.Flush();

        return ms.ToArray();
    }
}
