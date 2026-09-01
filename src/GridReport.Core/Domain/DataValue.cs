namespace GridReport.Core.Domain;

public sealed record DataValue(
    string Field,
    string Value,
    DataProvenance Provenance,
    FieldStatus Status,
    bool IsConfirmed = false,
    bool IsCritical = false)
{
    public static DataValue Auto(string field, string value, DataProvenance provenance, bool isCritical = false) =>
        new(field, value, provenance, FieldStatus.AutoDetected, false, isCritical);

    public static DataValue Manual(string field, string value, string note, bool isCritical = false) =>
        new(field, value, DataProvenance.Manual(note), FieldStatus.Manual, true, isCritical);

    public DataValue Confirm() => this with { Status = FieldStatus.Confirmed, IsConfirmed = true };
}
