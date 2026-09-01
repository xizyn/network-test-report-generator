using System.Text.Json;
using GridReport.Core.Domain;

namespace GridReport.Core.Services;

public sealed class ProjectStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public void Save(GridProject project, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(project, Options));
    }

    public GridProject Load(string path) => JsonSerializer.Deserialize<GridProject>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("项目文件无效。");
}
