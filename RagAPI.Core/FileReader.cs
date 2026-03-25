using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RagAPI.Core.Providers;
using UglyToad.PdfPig;

namespace RagAPI.Core;

public static class FileReader
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif"];

    public static async Task<string> ReadAsync(string filePath, ILlmProvider llmProvider)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ImageExtensions.Contains(ext))
        {
            return await ReadImageAsync(filePath, llmProvider);
        }

        return ext switch
        {
            ".txt" or ".md" => File.ReadAllText(filePath),
            ".pdf" => await ReadPdfAsync(filePath, llmProvider),
            ".docx" => ReadDocx(filePath),
            _ => throw new NotSupportedException(
                                   $"Unsupported file type: {ext}. Supported: .txt .md .pdf .docx .jpg .jpeg .png .bmp .tiff")
        };
    }

    private static async Task<string> ReadImageAsync(string filePath, ILlmProvider llmProvider)
    {
        var ocrText = OcrReader.ReadImage(filePath);

        if (OcrReader.HasMeaningfulText(ocrText))
        {
            return ocrText;
        }

        return await llmProvider.DescribeImageAsync(filePath,
            "Describe this image in detail. Include colors, objects, text, people, and any other relevant visual information.");
    }

    private static async Task<string> ReadPdfAsync(string filePath, ILlmProvider llmProvider)
    {
        var sb = new StringBuilder();

        using var doc = PdfDocument.Open(filePath);
        foreach (var page in doc.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
            else
            {
                var ocrText = OcrReader.ReadScannedPdf(filePath);
                if (OcrReader.HasMeaningfulText(ocrText))
                {
                    sb.AppendLine(ocrText);
                }
                else
                {
                    var description = await llmProvider.DescribeImageAsync(filePath,
                        "Describe all text and visual content in this document page in detail.");
                    sb.AppendLine(description);
                }
                break;
            }
        }

        return sb.ToString();
    }

    private static string ReadDocx(string filePath)
    {
        var sb = new StringBuilder();

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("Could not read DOCX body — file may be corrupt or empty.");

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }

        return sb.ToString();
    }
}