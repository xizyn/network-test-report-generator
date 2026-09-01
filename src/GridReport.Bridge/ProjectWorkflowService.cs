using GridReport.Core.Domain;
using GridReport.Core.Mapping;
using GridReport.Core.Reporting;
using GridReport.Core.Services;
using GridReport.Core.Template;
using GridReport.Core.Validation;

namespace GridReport.Bridge;

public sealed class ProjectWorkflowService
{
    private readonly BridgeProjectIndex _index;
    private readonly ProjectStore _store = new();
    private readonly FileTypeDetector _detector = new();
    private readonly DocxTemplateScanner _templateScanner = new();
    private readonly FieldMappingEngine _mappingEngine = new();
    private readonly PreflightValidator _preflight = new();
    private readonly DocxReportGenerator _generator = new();

    public ProjectWorkflowService(BridgeProjectIndex index) => _index = index;
    public RegisteredProject RegisterProject(string projectFile) => _index.Register(projectFile);
    public IReadOnlyList<ProjectSummaryResponse> ListProjects() => _index.List().Where(x => File.Exists(x.ProjectFile)).Select(x => ToSummary(x, Load(x))).ToList();
    public ProjectResponse GetProject(string id) { var entry = _index.Get(id); var p = Load(entry); return new(entry.Id, p.Name, p.CustomerName, p.ProjectNumber, p.StationName, p.TestDate, p.TemplatePath, p.SourceFolder, p.OutputFolder); }

    public ScanResponse Scan(string id)
    {
        var entry = _index.Get(id); var project = Load(entry);
        if (string.IsNullOrWhiteSpace(project.SourceFolder)) throw new InvalidOperationException("项目尚未选择资料目录。");
        var records = new CustomerPackageScanner(_detector).Scan(project.SourceFolder);
        var values = new List<DataValue>();
        foreach (var record in records)
        {
            try
            {
                values.AddRange(record.Kind switch
                {
                    FileKind.Xlsx or FileKind.Xls => new ExcelDataExtractor().Extract(record.Path),
                    FileKind.Csv or FileKind.Txt => new DelimitedDataExtractor().Extract(record.Path),
                    _ => []
                });
            }
            catch { /* 单文件失败由扫描记录和后续人工处理承接，不中断资料包。 */ }
        }
        foreach (var value in values) project.SetValue(value);
        Save(entry, project);
        return new(records.Count, records.Count(x => x.Status == FileRecordStatus.Ready), records.Count(x => x.Status == FileRecordStatus.NeedsReview), records.Count(x => x.Status == FileRecordStatus.Failed), values.Count);
    }

    public IReadOnlyList<FieldResponse> GetFields(string id)
    {
        var project = Load(_index.Get(id));
        return project.Values.Values.OrderBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
            .Select(x => new FieldResponse(x.Field, x.Value, x.Provenance.FileName, x.Provenance.Location, x.Provenance.ExtractionMethod, x.Status.ToString(), x.IsConfirmed)).ToList();
    }

    public IReadOnlyList<MappingResponse> GetMappings(string id) => BuildMappings(_index.Get(id)).Select(x => new MappingResponse(x.Field.Name, x.Field.OriginalText, x.Value?.Value, x.Value?.Provenance.FileName, x.Value?.Provenance.Location, x.Status.ToString(), x.Value?.IsConfirmed ?? false, x.IsCritical)).ToList();

    public IReadOnlyList<MappingResponse> UpdateMapping(string id, MappingUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FieldName) || request.FieldName.Length > 120) throw new ArgumentException("字段名称无效。");
        if (string.IsNullOrWhiteSpace(request.Value) || request.Value.Length > 10000) throw new ArgumentException("字段值不能为空且不能超过 10000 个字符。");
        var entry = _index.Get(id); var project = Load(entry); var mappings = BuildMappings(entry);
        if (!mappings.Any(x => string.Equals(x.Field.Name, request.FieldName.Trim(), StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("字段不属于当前报告模板。");
        project.SetValue(new DataValue(request.FieldName.Trim(), request.Value.Trim(), DataProvenance.Manual("WPS 字段映射"), FieldStatus.Manual, request.Confirmed));
        Save(entry, project); return GetMappings(id);
    }

    public ProjectResponse SetTemplate(string id, TemplateUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplatePath) || request.TemplatePath.Contains("..", StringComparison.Ordinal)) throw new ArgumentException("模板路径无效，不能包含相对路径片段。\n");
        var path = Path.GetFullPath(request.TemplatePath);
        if (!path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw new ArgumentException("当前 WPS 文档必须是存在的 .docx 文件。\n");
        var entry = _index.Get(id); var project = Load(entry); project.TemplatePath = path; Save(entry, project);
        return GetProject(id);
    }

    public PreflightResponse Preflight(string id, string mode)
    {
        var entry = _index.Get(id); var project = Load(entry); var reportMode = ParseMode(mode);
        var result = _preflight.Validate(BuildMappings(entry), project.TemplatePath ?? string.Empty, BuildOutputPath(project), reportMode);
        return ToResponse(result);
    }

    public GenerateResponse Generate(string id, string mode)
    {
        var entry = _index.Get(id); var project = Load(entry); var reportMode = ParseMode(mode); var output = BuildOutputPath(project);
        var check = _preflight.Validate(BuildMappings(entry), project.TemplatePath ?? string.Empty, output, reportMode);
        if (!check.CanGenerate) return new(false, null, check.Issues.Where(x => x.Severity == PreflightSeverity.Error).Select(x => x.Message).ToList(), check.Issues.Where(x => x.Severity == PreflightSeverity.Warning).Select(x => x.Message).ToList());
        try
        {
            var result = _generator.Generate(project.TemplatePath!, output, BuildMappings(entry), reportMode);
            return new(true, result.OutputPath, [], result.Warnings);
        }
        catch (Exception ex) { return new(false, null, [ex.Message], []); }
    }

    private List<FieldMapping> BuildMappings(RegisteredProject entry)
    {
        var project = Load(entry);
        if (string.IsNullOrWhiteSpace(project.TemplatePath) || !File.Exists(project.TemplatePath)) return [];
        return _mappingEngine.Match(_templateScanner.Scan(project.TemplatePath), project.Values.Values).Select(x => x.Value?.IsConfirmed == true ? x with { Status = FieldStatus.Confirmed } : x).ToList();
    }
    private GridProject Load(RegisteredProject entry) => _store.Load(entry.ProjectFile);
    private void Save(RegisteredProject entry, GridProject project) => _store.Save(project, entry.ProjectFile);
    private static ProjectSummaryResponse ToSummary(RegisteredProject e, GridProject p) => new(e.Id, p.Name, p.CustomerName, p.TemplatePath, p.SourceFolder, p.OutputFolder);
    private static ReportMode ParseMode(string mode) => mode?.Trim().ToLowerInvariant() switch { "draft" => ReportMode.Draft, "formal" => ReportMode.Formal, _ => throw new ArgumentException("mode 必须是 draft 或 formal。") };
    private static PreflightResponse ToResponse(PreflightResult result) => new(result.CanGenerate, result.Issues.Where(x => x.Severity == PreflightSeverity.Error).ToList(), result.Issues.Where(x => x.Severity == PreflightSeverity.Warning).ToList(), result.Issues.Where(x => x.Severity == PreflightSeverity.Information).ToList());
    private static string BuildOutputPath(GridProject project)
    {
        if (string.IsNullOrWhiteSpace(project.OutputFolder)) throw new InvalidOperationException("项目尚未设置输出目录。");
        var folder = Path.GetFullPath(project.OutputFolder);
        var name = string.Concat((string.IsNullOrWhiteSpace(project.Name) ? "涉网试验" : project.Name).Select(x => Path.GetInvalidFileNameChars().Contains(x) ? '_' : x));
        return Path.Combine(folder, $"{DateTime.Today:yyyyMMdd}_{name}_涉网试验报告.docx");
    }
}
