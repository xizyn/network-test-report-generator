using System.Text;
using ExcelDataReader;
using GridReport.Core.Domain;

namespace GridReport.Core.Services;

public sealed class ExcelDataExtractor
{
    public List<DataValue> Extract(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var values = new List<DataValue>();
        do
        {
            var sheet = reader.Name; var row = 0;
            while (reader.Read())
            {
                row++;
                if (reader.FieldCount < 2) continue;
                var field = reader.GetValue(0)?.ToString()?.Trim();
                var value = reader.GetValue(1)?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value)) continue;
                values.Add(DataValue.Auto(field, value, new DataProvenance(Path.GetFileName(path), $"{sheet}!A{row}:B{row}", "Excel 单元格", field)));
            }
        } while (reader.NextResult());
        return values;
    }
}
