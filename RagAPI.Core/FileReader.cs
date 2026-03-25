using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace RagAPI.Core;

public static class FileReader
{
    public static string Read(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        //TODO: Expand this to include more file types in the future
        return extension switch
        {
            ".txt" or ".md" => File.ReadAllText(filePath),
            ".pdf" => ReadPdf(filePath),
            ".docx" => ReadDocx(filePath),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}. Supported types are: .txt, .md, .pdf, .docx")
        };
    }

    private static string ReadPdf(string filePath)
    {
        var stringBuilder = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            stringBuilder.AppendLine(page.Text);
        }

        return stringBuilder.ToString();
    }

    private static string ReadDocx(string filePath)
    {
        var stringBuilder = new StringBuilder();

        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body 
            ?? throw new InvalidOperationException("Could not read DOCX body. File may be corrupt or empty.");

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            stringBuilder.AppendLine(paragraph.InnerText);
        }

        return stringBuilder.ToString();
    }
}