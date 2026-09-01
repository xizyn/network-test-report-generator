using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GridReport.Core.Domain;
using GridReport.Core.Mapping;
using GridReport.Core.Reporting;
using GridReport.Core.Services;
using GridReport.Core.Template;
using GridReport.Core.Validation;
using Serilog;

namespace GridReport.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly FileTypeDetector _detector = new();
    private readonly ProjectStore _store = new();
    private readonly DocxTemplateScanner _templateScanner = new();
    private readonly FieldMappingEngine _mappingEngine = new();
    private readonly PreflightValidator _preflight = new();
    private readonly DocxReportGenerator _generator = new();
    private GridProject _project = new();
    private string _statusText = "请新建或打开一个项目。";

    public GridProject Project { get => _project; private set { _project = value; OnChanged(); OnChanged(nameof(ProjectTitle)); } }
    public string ProjectTitle => string.IsNullOrWhiteSpace(Project.Name) ? "未命名项目" : Project.Name;
    public string StatusText { get => _statusText; private set { _statusText = value; OnChanged(); } }
    public ObservableCollection<FileRecord> Files { get; } = [];
    public ObservableCollection<MappingRow> Mappings { get; } = [];
    public ObservableCollection<PreflightIssue> PreflightIssues { get; } = [];
    public List<DataValue> ExtractedValues { get; } = [];
    public string Summary => $"资料 {Files.Count} | 字段 {Mappings.Count} | 已确认 {Mappings.Count(x => x.Confirmed)} | 问题 {PreflightIssues.Count}";

    public void NewProject(string name, string customer, string projectNumber, string station, DateTime? testDate)
    {
        Project = new GridProject { Name = name, CustomerName = customer, ProjectNumber = projectNumber, StationName = station, TestDate = testDate, OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "涉网试验报告输出") };
        Files.Clear(); Mappings.Clear(); PreflightIssues.Clear(); ExtractedValues.Clear();
        StatusText = "项目已创建，请选择客户资料目录。"; Log.Information("创建项目 {ProjectName}", name);
    }

    public void ScanFolder(string folder)
    {
        Project.SourceFolder = folder; Files.Clear(); ExtractedValues.Clear();
        foreach (var record in new CustomerPackageScanner(_detector).Scan(folder))
        {
            Files.Add(record);
            try { ExtractedValues.AddRange(record.Kind switch { FileKind.Xlsx or FileKind.Xls => new ExcelDataExtractor().Extract(record.Path), FileKind.Csv or FileKind.Txt => new DelimitedDataExtractor().Extract(record.Path), _ => [] }); }
            catch (Exception ex) { Log.Warning(ex, "无法提取资料 {File}", record.Path); }
        }
        foreach (var value in ExtractedValues) Project.SetValue(value);
        StatusText = $"已扫描 {Files.Count} 个文件，提取 {ExtractedValues.Count} 个候选数据。"; OnChanged(nameof(Summary)); Log.Information("扫描资料目录 {Folder}，发现 {Count} 文件", folder, Files.Count);
    }

    public void LoadTemplate(string path)
    {
        Project.TemplatePath = path; PopulateMappings(_mappingEngine.Match(_templateScanner.Scan(path), ExtractedValues));
        StatusText = $"已扫描模板：{Mappings.Count} 个批注字段。"; Log.Information("扫描模板 {Template}，发现 {Count} 字段", path, Mappings.Count);
    }

    public void ReMatch()
    {
        if (string.IsNullOrWhiteSpace(Project.TemplatePath)) throw new InvalidOperationException("请先选择报告模板。");
        PopulateMappings(_mappingEngine.Match(_templateScanner.Scan(Project.TemplatePath), ExtractedValues)); StatusText = "已按精确名称和别名重新匹配。";
    }

    public PreflightResult Check(string outputPath, ReportMode mode)
    {
        var result = _preflight.Validate(ToMappings(), Project.TemplatePath ?? string.Empty, outputPath, mode);
        PreflightIssues.Clear(); foreach (var issue in result.Issues) PreflightIssues.Add(issue);
        StatusText = result.CanGenerate ? "报告校核通过。" : $"报告校核发现 {result.Issues.Count} 个问题。"; OnChanged(nameof(Summary)); return result;
    }

    public ReportGenerationResult Generate(string outputPath, ReportMode mode)
    {
        var check = Check(outputPath, mode);
        if (!check.CanGenerate) throw new InvalidOperationException("报告校核未通过；请修改映射，或选择生成草稿。\n" + string.Join("\n", check.Issues.Select(x => x.Message)));
        var result = _generator.Generate(Project.TemplatePath!, outputPath, ToMappings(), mode);
        StatusText = $"已生成报告副本：替换 {result.ReplacedCount} 处字段。"; Log.Information("生成报告 {Output}，替换 {Count} 处", outputPath, result.ReplacedCount); return result;
    }

    public void Save(string projectFile) { _store.Save(Project, projectFile); StatusText = "项目已保存。"; Log.Information("保存项目 {File}", projectFile); }
    public void Open(string projectFile) { Project = _store.Load(projectFile); Files.Clear(); Mappings.Clear(); PreflightIssues.Clear(); ExtractedValues.Clear(); StatusText = "项目已打开。请重新扫描资料并加载模板以刷新工作区。"; Log.Information("打开项目 {File}", projectFile); }

    private void PopulateMappings(IEnumerable<FieldMapping> mappings)
    {
        Mappings.Clear(); foreach (var mapping in mappings) Mappings.Add(new MappingRow { Mapping = mapping, Value = mapping.Value?.Value ?? string.Empty, Status = mapping.Status, Confirmed = mapping.Value?.IsConfirmed ?? false }); OnChanged(nameof(Summary));
    }

    private List<FieldMapping> ToMappings() => Mappings.Select(row =>
    {
        if (string.IsNullOrWhiteSpace(row.Value)) return row.Mapping with { Value = null, Status = FieldStatus.Missing };
        var value = DataValue.Manual(row.Field, row.Value, row.SourceLocation is { Length: > 0 } ? row.SourceLocation : "字段映射界面", row.Mapping.IsCritical);
        var status = row.Confirmed ? FieldStatus.Confirmed : (row.Status == FieldStatus.ExactMatched ? FieldStatus.ExactMatched : FieldStatus.Manual);
        return new FieldMapping(row.Mapping.Field, value, status, row.Mapping.IsCritical);
    }).ToList();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
