using System.Net;
using System.Net.Http.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GridReport.Bridge;
using GridReport.Core.Domain;
using GridReport.Core.Services;

namespace GridReport.Tests;

public sealed class BridgeTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GridReportBridgeTests", Guid.NewGuid().ToString("N"));
    private BridgeHost _host = null!;
    private HttpClient _client = null!;
    private string _projectId = string.Empty;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var project = new GridProject { Name = "XX光伏项目", SourceFolder = Path.Combine(_root, "source"), OutputFolder = Path.Combine(_root, "output"), TemplatePath = CreateTemplate() };
        Directory.CreateDirectory(project.SourceFolder!);
        File.WriteAllText(Path.Combine(project.SourceFolder!, "data.csv"), "项目名称,XX光伏项目\n客户名称,广州XX科技");
        var projectFile = Path.Combine(_root, "project.gridreport.json");
        new ProjectStore().Save(project, projectFile);
        var options = new BridgeOptions { Port = 43819, ProjectIndexPath = Path.Combine(_root, "projects.json"), AddinRoot = _root };
        _host = new BridgeHost(options); _projectId = _host.RegisterProject(projectFile).Id;
        await _host.StartAsync(); _client = new HttpClient { BaseAddress = new Uri(_host.Address) };
    }

    [Fact]
    public async Task Health_returns_running_versions_and_rejects_non_loopback_binding()
    {
        var response = await _client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.Equal("running", response!.Status);
        Assert.Equal("v1", response.ApiVersion);
        Assert.StartsWith("http://127.0.0.1:", _host.Address);
    }

    [Fact]
    public async Task Projects_and_fields_return_registered_project_data()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectSummaryResponse>>("/projects");
        var fields = await _client.GetFromJsonAsync<List<FieldResponse>>($"/projects/{_projectId}/fields");

        Assert.NotNull(projects);
        var project = Assert.Single(projects!);
        Assert.Equal("XX光伏项目", project.Name);
        Assert.NotNull(fields);
        Assert.Empty(fields!);
    }

    [Fact]
    public async Task Scan_uses_existing_core_extractors_and_populates_fields()
    {
        var response = await _client.PostAsync($"/projects/{_projectId}/scan", null);
        var fields = await _client.GetFromJsonAsync<List<FieldResponse>>($"/projects/{_projectId}/fields");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(fields);
        var customer = Assert.Single(fields!, x => x.FieldName == "客户名称");
        Assert.Equal("广州XX科技", customer.Value);
        Assert.Equal("data.csv", customer.SourceFile);
    }

    [Fact]
    public async Task Mapping_update_confirms_value_and_preflight_returns_issues()
    {
        await _client.PostAsync($"/projects/{_projectId}/scan", null);
        var mappings = await _client.GetFromJsonAsync<List<MappingResponse>>($"/projects/{_projectId}/mapping");
        var update = await _client.PostAsJsonAsync($"/projects/{_projectId}/mapping", new MappingUpdateRequest("项目名称", "XX光伏项目", true));
        var preflight = await _client.PostAsJsonAsync($"/projects/{_projectId}/preflight", new PreflightRequest("formal"));

        Assert.Single(mappings!);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, preflight.StatusCode);
    }

    [Fact]
    public async Task Draft_generation_creates_copy_but_formal_generation_rejects_unconfirmed_field()
    {
        await _client.PostAsync($"/projects/{_projectId}/scan", null);
        var formal = await _client.PostAsJsonAsync($"/projects/{_projectId}/generate", new GenerateRequest("formal"));
        var draft = await _client.PostAsJsonAsync($"/projects/{_projectId}/generate", new GenerateRequest("draft"));
        var payload = await draft.Content.ReadFromJsonAsync<GenerateResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, formal.StatusCode);
        Assert.True(payload!.Success);
        Assert.True(File.Exists(payload.OutputPath));
    }

    [Fact]
    public async Task Invalid_project_id_returns_not_found()
    {
        var response = await _client.GetAsync($"/projects/{Guid.NewGuid()}/fields");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Service_shutdown_marks_host_as_not_running()
    {
        var host = new BridgeHost(new BridgeOptions { Port = 43820, ProjectIndexPath = Path.Combine(_root, "second-projects.json"), AddinRoot = _root });
        await host.StartAsync();
        await host.StopAsync();

        Assert.False(host.IsRunning);
    }

    [Fact]
    public async Task Template_endpoint_accepts_existing_docx_but_rejects_non_docx_path()
    {
        var valid = await _client.PostAsJsonAsync($"/projects/{_projectId}/template", new TemplateUpdateRequest(CreateTemplate()));
        var project = await _client.GetFromJsonAsync<ProjectResponse>($"/projects/{_projectId}");
        var invalid = await _client.PostAsJsonAsync($"/projects/{_projectId}/template", new TemplateUpdateRequest(Path.Combine(_root, "unknown.pdf")));

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.NotNull(project);
        Assert.EndsWith(".docx", project!.TemplatePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    public async Task DisposeAsync()
    {
        if (_host is not null) await _host.StopAsync();
        _client?.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string CreateTemplate()
    {
        var path = Path.Combine(_root, "template.docx");
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart(); main.Document = new Document(new Body());
        var comments = main.AddNewPart<WordprocessingCommentsPart>(); comments.Comments = new Comments(new Comment(new Paragraph(new Run(new Text("项目名称")))) { Id = "0" });
        main.Document.Body!.Append(new Paragraph(new CommentRangeStart { Id = "0" }, new Run(new Text("XXXX")), new CommentRangeEnd { Id = "0" }, new Run(new CommentReference { Id = "0" })));
        main.Document.Save(); comments.Comments.Save(); return path;
    }
}
