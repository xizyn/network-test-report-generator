namespace GridReport.Core.Domain;

public sealed class GridProject
{
    public string Name { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public DateTime? TestDate { get; set; }
    public string? TemplatePath { get; set; }
    public string? SourceFolder { get; set; }
    public string? OutputFolder { get; set; }
    public Dictionary<string, DataValue> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AuditEntry> AuditEntries { get; init; } = [];

    public void SetValue(DataValue value)
    {
        if (Values.TryGetValue(value.Field, out var old))
            AuditEntries.Add(new AuditEntry(value.Field, old.Value, value.Value, DateTimeOffset.Now, value.Provenance.Location));
        Values[value.Field] = value;
    }
}

public sealed record AuditEntry(string Field, string OldValue, string NewValue, DateTimeOffset Timestamp, string Reason);
