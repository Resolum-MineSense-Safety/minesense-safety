using System.Globalization;
using System.Text;

namespace ReportPdfGenerator.Pdf;

/// <summary>
/// Minimal PDF 1.4 writer (A4, Helvetica, WinAnsi) with automatic page breaks.
/// Enough for tabular compliance reports without native dependencies, which
/// keeps the Lambda package small and its cold start short.
/// </summary>
public class PdfDocumentWriter
{
    public const double PageWidth = 595.28;
    public const double PageHeight = 841.89;
    public const double Margin = 50;

    private static readonly Encoding WinAnsi = CreateWinAnsi();
    private readonly List<StringBuilder> _pages = [];
    private StringBuilder _current = null!;
    private double _cursorY;

    public PdfDocumentWriter() => NewPage();

    public int PageCount => _pages.Count;

    public void Heading(string text) => Line(text, 14, bold: true, spacingBefore: 10);

    public void Title(string text) => Line(text, 18, bold: true);

    public void Paragraph(string text, double size = 10)
    {
        foreach (var line in Wrap(text, size, PageWidth - 2 * Margin))
            Line(line, size);
    }

    /// <summary>Fixed-width columns; long cells are truncated to keep rows on one line.</summary>
    public void Table(IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string>> rows, double size = 9)
    {
        var columnWidth = (PageWidth - 2 * Margin) / columns.Count;
        Row(columns, columnWidth, size, bold: true);
        foreach (var row in rows)
            Row(row, columnWidth, size, bold: false);
    }

    public void Line(string text, double size, bool bold = false, double spacingBefore = 0)
    {
        var lineHeight = size * 1.4;
        EnsureSpace(lineHeight + spacingBefore);
        _cursorY -= lineHeight + spacingBefore;
        WriteText(Margin, _cursorY, text, size, bold);
    }

    public byte[] ToBytes()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "", // pages tree, filled once the page ids are known
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };

        var pageIds = new List<int>();
        foreach (var page in _pages)
        {
            var content = page.ToString();
            objects.Add($"<< /Length {WinAnsi.GetByteCount(content)} >>\nstream\n{content}endstream");
            var contentId = objects.Count;
            objects.Add(string.Create(CultureInfo.InvariantCulture,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
            pageIds.Add(objects.Count);
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Count} >>";

        using var stream = new MemoryStream();
        var offsets = new List<long>();
        Write(stream, "%PDF-1.4\n");
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = stream.Position;
        var table = new StringBuilder($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            table.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        table.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        Write(stream, table.ToString());
        return stream.ToArray();
    }

    /// <summary>Escapes PDF string delimiters and replaces characters WinAnsi cannot encode.</summary>
    public static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            var safe = WinAnsi.GetString(WinAnsi.GetBytes(character.ToString()));
            builder.Append(character switch
            {
                '\\' => "\\\\",
                '(' => "\\(",
                ')' => "\\)",
                '\r' or '\n' or '\t' => " ",
                _ => safe
            });
        }
        return builder.ToString();
    }

    private void Row(IReadOnlyList<string> cells, double columnWidth, double size, bool bold)
    {
        var lineHeight = size * 1.5;
        EnsureSpace(lineHeight);
        _cursorY -= lineHeight;
        for (var index = 0; index < cells.Count; index++)
        {
            var maxChars = Math.Max(3, (int)(columnWidth / (size * 0.5)) - 1);
            var cell = cells[index].Length > maxChars ? cells[index][..(maxChars - 1)] + "~" : cells[index];
            WriteText(Margin + index * columnWidth, _cursorY, cell, size, bold);
        }
    }

    private void WriteText(double x, double y, string text, double size, bool bold) =>
        _current.Append(string.Create(CultureInfo.InvariantCulture,
            $"BT /{(bold ? "F2" : "F1")} {size} Tf {x:0.##} {y:0.##} Td ({Escape(text)}) Tj ET\n"));

    private void EnsureSpace(double height)
    {
        if (_cursorY - height < Margin) NewPage();
    }

    private void NewPage()
    {
        _current = new StringBuilder();
        _pages.Add(_current);
        _cursorY = PageHeight - Margin;
    }

    private static IEnumerable<string> Wrap(string text, double size, double width)
    {
        var maxChars = Math.Max(10, (int)(width / (size * 0.5)));
        var line = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > maxChars)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
    }

    private static void Write(Stream stream, string text)
    {
        var bytes = WinAnsi.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static Encoding CreateWinAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, new EncoderReplacementFallback("?"), DecoderFallback.ReplacementFallback);
    }
}
