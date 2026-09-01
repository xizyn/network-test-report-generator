using GridReport.Core.Domain;
using GridReport.Core.Template;

namespace GridReport.Core.Mapping;

public sealed record FieldMapping(TemplateField Field, DataValue? Value, FieldStatus Status, bool IsCritical = false)
{
    public FieldMapping Confirm() => this with { Status = FieldStatus.Confirmed };
}
