using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace GridReport.Core.Template;

public sealed class DocxTemplateScanner
{
    public List<TemplateField> Scan(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var main = document.MainDocumentPart ?? throw new InvalidDataException("DOCX 缺少主文档部分。");
        var comments = main.WordprocessingCommentsPart?.Comments?.Elements<Comment>()
            .Where(x => !string.IsNullOrWhiteSpace(x.Id?.Value))
            .ToDictionary(x => x.Id!.Value!, x => x.InnerText.Trim()) ?? [];
        if (comments.Count == 0) return [];
        var fields = new List<TemplateField>();
        AddFromStory(main.Document, "main", comments, fields);
        foreach (var header in main.HeaderParts) AddFromStory(header.Header, header.Uri.ToString(), comments, fields);
        foreach (var footer in main.FooterParts) AddFromStory(footer.Footer, footer.Uri.ToString(), comments, fields);
        return fields;
    }

    private static void AddFromStory(OpenXmlElement? root, string scope, IReadOnlyDictionary<string, string> comments, ICollection<TemplateField> output)
    {
        if (root is null) return;
        foreach (var start in root.Descendants<CommentRangeStart>())
        {
            var id = start.Id?.Value;
            if (string.IsNullOrWhiteSpace(id) || !comments.TryGetValue(id, out var name) || string.IsNullOrWhiteSpace(name)) continue;
            var text = ReadRangeText(root, id);
            output.Add(new TemplateField(id, name, text, scope, IsImageField(name)));
        }
    }

    internal static string ReadRangeText(OpenXmlElement root, string id)
    {
        var inRange = false; var text = new System.Text.StringBuilder();
        foreach (var element in root.Descendants())
        {
            if (element is CommentRangeStart start && start.Id?.Value == id) { inRange = true; continue; }
            if (element is CommentRangeEnd end && end.Id?.Value == id) break;
            if (inRange && element is Text value) text.Append(value.Text);
        }
        return text.ToString();
    }

    private static bool IsImageField(string field) => field.Contains("照片", StringComparison.Ordinal) || field.Contains("图片", StringComparison.Ordinal) || field.Contains("图纸", StringComparison.Ordinal) || field.Contains("铭牌", StringComparison.Ordinal);
}
