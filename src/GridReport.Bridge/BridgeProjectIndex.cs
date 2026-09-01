using System.Text.Json;

namespace GridReport.Bridge;

public sealed class BridgeProjectIndex(string indexPath)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _indexPath = Path.GetFullPath(indexPath);

    public RegisteredProject Register(string projectFile)
    {
        var fullPath = Path.GetFullPath(projectFile);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("项目文件不存在。", fullPath);
        var entries = Load();
        var existing = entries.FirstOrDefault(x => string.Equals(x.ProjectFile, fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var created = new RegisteredProject(Guid.NewGuid().ToString("D"), fullPath);
        entries.Add(created); Save(entries); return created;
    }

    public IReadOnlyList<RegisteredProject> List() => Load();

    public RegisteredProject Get(string id)
    {
        if (!Guid.TryParse(id, out _)) throw new UnknownProjectException(id);
        return Load().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase) && File.Exists(x.ProjectFile)) ?? throw new UnknownProjectException(id);
    }

    private List<RegisteredProject> Load()
    {
        if (!File.Exists(_indexPath)) return [];
        return JsonSerializer.Deserialize<List<RegisteredProject>>(File.ReadAllText(_indexPath), Json) ?? [];
    }

    private void Save(List<RegisteredProject> entries)
    {
        var directory = Path.GetDirectoryName(_indexPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(entries, Json));
    }
}
