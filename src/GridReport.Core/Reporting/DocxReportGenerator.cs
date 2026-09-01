using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GridReport.Core.Domain;
using GridReport.Core.Mapping;
using GridReport.Core.Validation;

namespace GridReport.Core.Reporting;

public sealed record ReportGenerationResult(string OutputPath, int ReplacedCount, IReadOnlyList<string> Warnings);

public sealed class DocxReportGenerator
{
    public ReportGenerationResult Generate(string templatePath, string outputPath, IEnumerable<FieldMapping> mappings, ReportMode mode)
    {
        if (!File.Exists(templatePath)) throw new FileNotFoundException("报告模板不存在。", templatePath);
        if (File.Exists(outputPath)) throw new IOException("输出文件已存在，拒绝静默覆盖。");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.Copy(templatePath, outputPath, false);
        var warnings = new List<string>(); var replaced = 0;
        using var document = WordprocessingDocument.Open(outputPath, true);
        var main = document.MainDocumentPart ?? throw new InvalidDataException("DOCX 缺少主文档部分。");
        foreach (var mapping in mappings.Where(x => x.Value is not null && x.Status is not FieldStatus.Conflict and not FieldStatus.Missing))
        {
            var root = FindRoot(main, mapping.Field.Scope);
            if (root is null || !ReplaceText(root, mapping.Field.CommentId, mapping.Value!.Value)) warnings.Add($"字段“{mapping.Field.Name}”找不到可替换的批注范围。");
            else replaced++;
        }
        main.Document?.Save();
        foreach (var header in main.HeaderParts) header.Header?.Save();
        foreach (var footer in main.FooterParts) footer.Footer?.Save();
        return new(outputPath, replaced, warnings);
    }

    private static OpenXmlElement? FindRoot(MainDocumentPart main, string scope)
    {
        if (scope == "main") return main.Document;
        var header = main.HeaderParts.FirstOrDefault(x => x.Uri.ToString() == scope);
        if (header is not null) return header.Header;
        return main.FooterParts.FirstOrDefault(x => x.Uri.ToString() == scope)?.Footer;
    }

    private static bool ReplaceText(OpenXmlElement root, string id, string replacement)
    {
        var elements = root.Descendants().ToList();
        var start = elements.FindIndex(x => x is CommentRangeStart range && range.Id?.Value == id);
        var end = elements.FindIndex(start + 1, x => x is CommentRangeEnd range && range.Id?.Value == id);
        if (start < 0 || end <= start) return false;
        var texts = elements.Skip(start + 1).Take(end - start - 1).OfType<Text>().ToList();
        if (texts.Count == 0) return false;
        texts[0].Text = replacement;
        texts[0].Space = SpaceProcessingModeValues.Preserve;
        foreach (var text in texts.Skip(1)) text.Text = string.Empty;
        return true;
    }
}
