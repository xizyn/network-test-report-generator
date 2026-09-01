using GridReport.Core.Domain;
using GridReport.Core.Mapping;

namespace GridReport.Desktop.ViewModels;

public sealed class MappingRow
{
    public required FieldMapping Mapping { get; init; }
    public string Field => Mapping.Field.Name;
    public string TemplateText => Mapping.Field.OriginalText;
    public string Value { get; set; } = string.Empty;
    public string Source => Mapping.Value?.Provenance.FileName ?? string.Empty;
    public string SourceLocation => Mapping.Value?.Provenance.Location ?? string.Empty;
    public FieldStatus Status { get; set; }
    public bool Confirmed { get; set; }
}
