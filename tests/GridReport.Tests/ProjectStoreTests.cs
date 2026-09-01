using GridReport.Core.Domain;
using GridReport.Core.Services;

namespace GridReport.Tests;

public sealed class ProjectStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "GridReportTests", Guid.NewGuid().ToString("N"));
    public ProjectStoreTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void Store_round_trips_project_values_and_audit_history()
    {
        var project = new GridProject { Name = "XX光伏涉网试验项目", CustomerName = "广州XX科技" };
        project.SetValue(DataValue.Manual("项目名称", "XX光伏涉网试验项目", "工程师确认"));
        project.SetValue(DataValue.Manual("项目名称", "XX光伏涉网试验项目（修订）", "工程师修订"));
        var path = Path.Combine(_folder, "project.gridreport.json");

        var store = new ProjectStore();
        store.Save(project, path);
        var loaded = store.Load(path);

        Assert.Equal("广州XX科技", loaded.CustomerName);
        Assert.Equal("XX光伏涉网试验项目（修订）", loaded.Values["项目名称"].Value);
        Assert.Single(loaded.AuditEntries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
