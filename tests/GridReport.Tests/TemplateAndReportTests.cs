using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GridReport.Core.Domain;
using GridReport.Core.Mapping;
using GridReport.Core.Reporting;
using GridReport.Core.Template;
using GridReport.Core.Validation;

namespace GridReport.Tests;

public sealed class TemplateAndReportTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "GridReportTests", Guid.NewGuid().ToString("N"));
    public TemplateAndReportTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void Scanner_reads_chinese_comment_and_cross_run_anchor()
    {
        var template = CreateTemplate(("0", "项目名称", new[] { "XX", "XXXX" }));

        var fields = new DocxTemplateScanner().Scan(template);

        var field = Assert.Single(fields);
        Assert.Equal("项目名称", field.Name);
        Assert.Equal("XXXXXX", field.OriginalText);
    }

    [Fact]
    public void Scanner_reads_comment_anchor_inside_a_table_cell()
    {
        var path = Path.Combine(_folder, "table-template.docx");
        using (var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart(); main.Document = new Document(new Body());
            var comments = main.AddNewPart<WordprocessingCommentsPart>(); comments.Comments = new Comments(new Comment(new Paragraph(new Run(new Text("逆变器型号")))) { Id = "7" });
            var anchor = new Paragraph(new CommentRangeStart { Id = "7" }, new Run(new Text("XXXX")), new CommentRangeEnd { Id = "7" }, new Run(new CommentReference { Id = "7" }));
            main.Document.Body!.Append(new Table(new TableRow(new TableCell(anchor)))); main.Document.Save(); comments.Comments.Save();
        }

        var fields = new DocxTemplateScanner().Scan(path);

        var field = Assert.Single(fields);
        Assert.Equal("逆变器型号", field.Name);
        Assert.Equal("XXXX", field.OriginalText);
    }

    [Fact]
    public void Mapping_engine_exactly_matches_repeated_template_fields()
    {
        var fields = new[]
        {
            new TemplateField("0", "客户名称", "XXXX", "main"),
            new TemplateField("1", "客户名称", "XXXX", "main")
        };
        var value = DataValue.Auto("客户名称", "广州XX科技有限公司", DataProvenance.Manual("测试")).Confirm();

        var mappings = new FieldMappingEngine().Match(fields, new[] { value });

        Assert.Equal(2, mappings.Count);
        Assert.All(mappings, m => Assert.Equal(FieldStatus.ExactMatched, m.Status));
    }

    [Fact]
    public void Generator_creates_copy_replaces_every_anchor_and_keeps_template_unchanged()
    {
        var template = CreateTemplate(("0", "项目名称", new[] { "XX", "XXXX" }), ("1", "项目名称", new[] { "XXXXXX" }));
        var before = File.ReadAllBytes(template);
        var fields = new DocxTemplateScanner().Scan(template);
        var value = DataValue.Manual("项目名称", "XX光伏涉网试验项目", "测试");
        var mappings = new FieldMappingEngine().Match(fields, new[] { value });
        var output = Path.Combine(_folder, "output.docx");

        new DocxReportGenerator().Generate(template, output, mappings, ReportMode.Draft);

        Assert.Equal(before, File.ReadAllBytes(template));
        Assert.True(File.Exists(output));
        Assert.Equal(2, new DocxTemplateScanner().Scan(output).Count);
        using var doc = WordprocessingDocument.Open(output, false);
        Assert.Contains("XX光伏涉网试验项目", doc.MainDocumentPart!.Document!.InnerText);
    }

    [Fact]
    public void Preflight_blocks_formal_report_when_critical_field_is_missing()
    {
        var missing = new FieldMapping(new TemplateField("0", "项目名称", "XXXX", "main"), null, FieldStatus.Missing, true);

        var result = new PreflightValidator().Validate(new[] { missing }, "template.docx", "report.docx", ReportMode.Formal);

        Assert.False(result.CanGenerate);
        Assert.Contains(result.Issues, x => x.Severity == PreflightSeverity.Error);
    }

    private string CreateTemplate(params (string id, string field, string[] textRuns)[] comments)
    {
        var path = Path.Combine(_folder, $"{Guid.NewGuid():N}.docx");
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new Document(new Body());
        var commentPart = main.AddNewPart<WordprocessingCommentsPart>();
        commentPart.Comments = new Comments();
        foreach (var (id, field, textRuns) in comments)
        {
            commentPart.Comments.Append(new Comment(new Paragraph(new Run(new Text(field)))) { Id = id });
            var paragraph = new Paragraph(new CommentRangeStart { Id = id });
            foreach (var text in textRuns) paragraph.Append(new Run(new Text(text)));
            paragraph.Append(new CommentRangeEnd { Id = id });
            paragraph.Append(new Run(new CommentReference { Id = id }));
            main.Document.Body!.Append(paragraph);
        }
        main.Document.Save();
        commentPart.Comments.Save();
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
