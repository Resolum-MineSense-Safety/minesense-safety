using System.Text;
using ReportPdfGenerator.Pdf;

namespace ReportPdfGenerator.Tests;

public class PdfDocumentWriterTests
{
    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    [Fact]
    public void ToBytes_SinglePage_ProducesWellFormedPdf()
    {
        // Arrange
        var writer = new PdfDocumentWriter();
        writer.Title("Reporte");

        // Act
        var pdf = Latin1(writer.ToBytes());

        // Assert
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF\n", pdf);
        Assert.Contains("/Count 1", pdf);
        Assert.Contains("(Reporte) Tj", pdf);
    }

    [Fact]
    public void ToBytes_XrefOffsetsPointToTheirObjects()
    {
        // Arrange
        var writer = new PdfDocumentWriter();
        writer.Paragraph("Contenido de prueba");
        var bytes = writer.ToBytes();
        var pdf = Latin1(bytes);

        // Act
        var startXref = int.Parse(pdf[(pdf.LastIndexOf("startxref", StringComparison.Ordinal) + 10)..].Split('\n')[0]);
        var entries = pdf[startXref..].Split('\n').Skip(3).TakeWhile(line => line.EndsWith(" n ")).ToList();

        // Assert
        Assert.StartsWith("xref", pdf[startXref..]);
        for (var index = 0; index < entries.Count; index++)
            Assert.StartsWith($"{index + 1} 0 obj", pdf[int.Parse(entries[index][..10])..]);
    }

    [Fact]
    public void Paragraph_LongContent_BreaksIntoSeveralPages()
    {
        // Arrange
        var writer = new PdfDocumentWriter();

        // Act
        for (var i = 0; i < 120; i++) writer.Paragraph($"Línea número {i}");
        var pdf = Latin1(writer.ToBytes());

        // Assert
        Assert.True(writer.PageCount > 1);
        Assert.Contains($"/Count {writer.PageCount}", pdf);
    }

    [Fact]
    public void Paragraph_LongText_WrapsWithinMargins()
    {
        // Arrange
        var writer = new PdfDocumentWriter();
        var text = string.Join(" ", Enumerable.Repeat("palabra", 60));

        // Act
        writer.Paragraph(text);
        var pdf = Latin1(writer.ToBytes());

        // Assert
        Assert.True(pdf.Split(" Tj ").Length - 1 > 1);
    }

    [Theory]
    [InlineData("a(b)c", "a\\(b\\)c")]
    [InlineData("C:\\ruta", "C:\\\\ruta")]
    [InlineData("línea\nnueva", "línea nueva")]
    [InlineData("emoji 😴", "emoji ??")]
    public void Escape_HandlesDelimitersAndUnsupportedCharacters(string input, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, PdfDocumentWriter.Escape(input));
    }

    [Fact]
    public void ToBytes_SpanishCharacters_AreEncodedAsWinAnsi()
    {
        // Arrange
        var writer = new PdfDocumentWriter();
        writer.Title("Atención");

        // Act
        var bytes = writer.ToBytes();

        // Assert: 'ó' is 0xF3 in WinAnsi
        Assert.Contains((byte)0xF3, bytes);
    }

    [Fact]
    public void Table_TruncatesCellsWiderThanTheirColumn()
    {
        // Arrange
        var writer = new PdfDocumentWriter();

        // Act
        writer.Table(["A", "B", "C", "D", "E", "F"], [[new string('x', 80), "1", "2", "3", "4", "5"]]);
        var pdf = Latin1(writer.ToBytes());

        // Assert
        Assert.Contains("~) Tj", pdf);
        Assert.DoesNotContain(new string('x', 80), pdf);
    }
}
