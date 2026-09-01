namespace GridReport.Core.Domain;

public sealed record DataProvenance(string FileName, string Location, string ExtractionMethod, string SourceField)
{
    public static DataProvenance Manual(string note) => new("人工输入", note, "Manual", note);
}
