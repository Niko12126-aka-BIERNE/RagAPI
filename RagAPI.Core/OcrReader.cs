using Tesseract;
using UglyToad.PdfPig;

namespace RagAPI.Core;

public static class OcrReader
{
    public const int MinWordCount = 20;

    private static readonly string TessDataPath = GetTessDataPath();

    private static string GetTessDataPath()
    {
        var linuxPath = "/usr/share/tesseract-ocr/5/tessdata";
        var windowsPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

        return Directory.Exists(linuxPath) ? linuxPath : windowsPath;
    }

    public static string ReadImage(string filePath, string languages = "eng+dan")
    {
        using var engine = new TesseractEngine(TessDataPath, languages, EngineMode.Default);
        using var img = Pix.LoadFromFile(filePath);
        using var page = engine.Process(img);

        return page.GetText();
    }

    public static string ReadScannedPdf(string filePath, string languages = "eng+dan")
    {
        var sb = new System.Text.StringBuilder();

        using var engine = new TesseractEngine(TessDataPath, languages, EngineMode.Default);
        using var doc = PdfDocument.Open(filePath);

        foreach (var page in doc.GetPages())
        {
            var pageImage = page.GetImages().FirstOrDefault();
            if (pageImage is null)
            {
                continue;
            }

            using var pix = Pix.LoadFromMemory(pageImage.RawBytes.ToArray());
            using var tessPage = engine.Process(pix);

            sb.AppendLine(tessPage.GetText());
        }

        return sb.ToString();
    }

    public static bool HasMeaningfulText(string text)
    {
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return wordCount >= MinWordCount;
    }
}