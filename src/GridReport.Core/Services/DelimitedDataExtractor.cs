using System.Text;
using GridReport.Core.Domain;

namespace GridReport.Core.Services;

public sealed class DelimitedDataExtractor
{
    public List<DataValue> Extract(string path)
    {
        var lines = File.ReadAllLines(path, DetectEncoding(path));
        var values = new List<DataValue>();
        for (var row = 0; row < lines.Length; row++)
        {
            var cells = Parse(lines[row]);
            if (cells.Count < 2 || string.IsNullOrWhiteSpace(cells[0])) continue;
            values.Add(DataValue.Auto(cells[0].Trim(), cells[1].Trim(), new DataProvenance(Path.GetFileName(path), $"A{row + 1}:B{row + 1}", "CSV/TXT", cells[0].Trim())));
        }
        return values;
    }

    private static Encoding DetectEncoding(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return new UTF8Encoding(true);
        return new UTF8Encoding(false, false);
    }

    private static List<string> Parse(string line)
    {
        var delimiter = line.Contains('\t') ? '\t' : ',';
        var result = new List<string>(); var builder = new StringBuilder(); var quoted = false;
        foreach (var character in line)
        {
            if (character == '"') { quoted = !quoted; continue; }
            if (character == delimiter && !quoted) { result.Add(builder.ToString()); builder.Clear(); }
            else builder.Append(character);
        }
        result.Add(builder.ToString());
        return result;
    }
}
