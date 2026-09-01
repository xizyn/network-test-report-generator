using GridReport.Core.Domain;
using GridReport.Core.Mapping;

namespace GridReport.Core.Validation;

public enum ReportMode { Draft, Formal }
public enum PreflightSeverity { Error, Warning, Information }
public sealed record PreflightIssue(PreflightSeverity Severity, string Code, string Message);
public sealed record PreflightResult(IReadOnlyList<PreflightIssue> Issues)
{
    public bool CanGenerate => Issues.All(x => x.Severity != PreflightSeverity.Error);
}

public sealed class PreflightValidator
{
    public PreflightResult Validate(IEnumerable<FieldMapping> mappings, string templatePath, string outputPath, ReportMode mode)
    {
        var issues = new List<PreflightIssue>();
        if (!File.Exists(templatePath)) issues.Add(new(PreflightSeverity.Error, "TEMPLATE_NOT_FOUND", "报告模板不存在。"));
        if (File.Exists(outputPath)) issues.Add(new(PreflightSeverity.Error, "OUTPUT_EXISTS", "输出文件已经存在，请另存为或明确覆盖。"));
        foreach (var mapping in mappings)
        {
            if (mapping.Status == FieldStatus.Conflict) issues.Add(new(PreflightSeverity.Error, "FIELD_CONFLICT", $"字段“{mapping.Field.Name}”存在多个数据来源。"));
            else if (mapping.Value is null && mapping.IsCritical) issues.Add(new(mode == ReportMode.Formal ? PreflightSeverity.Error : PreflightSeverity.Warning, "FIELD_MISSING", $"缺少必填字段“{mapping.Field.Name}”。"));
            else if (mapping.IsCritical && mode == ReportMode.Formal && mapping.Status is not FieldStatus.Confirmed and not FieldStatus.Manual)
                issues.Add(new(PreflightSeverity.Error, "FIELD_UNCONFIRMED", $"必填字段“{mapping.Field.Name}”尚未人工确认。"));
        }
        return new(issues);
    }
}
