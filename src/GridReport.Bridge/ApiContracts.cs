using GridReport.Core.Services;
using GridReport.Core.Validation;

namespace GridReport.Bridge;

public sealed class BridgeOptions
{
    public const int DefaultPort = 43801;
    public int Port { get; init; } = DefaultPort;
    public string ProjectIndexPath { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridReport", "bridge-projects.json");
    public string AddinRoot { get; init; } = Path.Combine(AppContext.BaseDirectory, "wps-addin");
}

public sealed record HealthResponse(string Status, string SoftwareVersion, string ApiVersion, DateTimeOffset? LastWpsRequest);
public sealed record ProjectSummaryResponse(string Id, string Name, string CustomerName, string? TemplatePath, string? SourceFolder, string? OutputFolder);
public sealed record ProjectResponse(string Id, string Name, string CustomerName, string ProjectNumber, string StationName, DateTime? TestDate, string? TemplatePath, string? SourceFolder, string? OutputFolder);
public sealed record ScanResponse(int FileCount, int ReadyCount, int NeedsReviewCount, int FailedCount, int ExtractedFieldCount);
public sealed record FieldResponse(string FieldName, string Value, string SourceFile, string SourceLocation, string ExtractionMethod, string Status, bool Confirmed);
public sealed record MappingResponse(string FieldName, string TemplateText, string? Value, string? SourceFile, string? SourceLocation, string Status, bool Confirmed, bool IsRequired);
public sealed record MappingUpdateRequest(string FieldName, string Value, bool Confirmed);
public sealed record TemplateUpdateRequest(string TemplatePath);
public sealed record PreflightRequest(string Mode);
public sealed record PreflightResponse(bool CanGenerate, IReadOnlyList<PreflightIssue> Errors, IReadOnlyList<PreflightIssue> Warnings, IReadOnlyList<PreflightIssue> Info);
public sealed record GenerateRequest(string Mode);
public sealed record GenerateResponse(bool Success, string? OutputPath, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
public sealed record RegisteredProject(string Id, string ProjectFile);
public sealed class UnknownProjectException(string id) : Exception($"项目不存在或不在本地 Bridge 索引内：{id}");
