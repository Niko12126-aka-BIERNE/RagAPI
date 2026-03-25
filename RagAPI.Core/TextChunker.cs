namespace RagAPI.Core;

public static class TextChunker
{
    public static List<string> Chunk(string text, int chunkSize, int overlap)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var start = 0;

        while (start < words.Length)
        {
            var chunk = string.Join(" ", words.Skip(start).Take(chunkSize)).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            start += chunkSize - overlap;
        }

        return chunks;
    }
}