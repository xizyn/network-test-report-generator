using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace GridReport.Bridge;

public sealed class BridgeHost : IAsyncDisposable
{
    private readonly BridgeOptions _options;
    private readonly ProjectWorkflowService _workflow;
    private WebApplication? _application;
    private DateTimeOffset? _lastWpsRequest;

    public BridgeHost(BridgeOptions options)
    {
        if (options.Port is < 1024 or > 65535) throw new ArgumentOutOfRangeException(nameof(options.Port), "Bridge 端口必须在 1024 到 65535 之间。");
        _options = options; _workflow = new ProjectWorkflowService(new BridgeProjectIndex(options.ProjectIndexPath));
    }

    public string Address => $"http://127.0.0.1:{_options.Port}";
    public bool IsRunning => _application is not null;
    public RegisteredProject RegisterProject(string projectFile) => _workflow.RegisterProject(projectFile);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null) return;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, _options.Port));
        var app = builder.Build();
        MapRoutes(app);
        await app.StartAsync(cancellationToken);
        _application = app;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_application is null) return;
        var app = _application; _application = null;
        await app.StopAsync(cancellationToken); await app.DisposeAsync();
    }

    private void MapRoutes(WebApplication app)
    {
        app.Use(async (context, next) => { if (string.Equals(context.Request.Headers["X-GridReport-Client"], "wps", StringComparison.OrdinalIgnoreCase)) _lastWpsRequest = DateTimeOffset.UtcNow; await next(); });
        app.MapGet("/health", () => Results.Ok(new HealthResponse("running", Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0", "v1", _lastWpsRequest)));
        app.MapGet("/projects", () => Results.Ok(_workflow.ListProjects()));
        app.MapGet("/projects/{id:guid}", (Guid id) => Invoke(() => Results.Ok(_workflow.GetProject(id.ToString()))));
        app.MapPost("/projects/{id:guid}/scan", (Guid id) => Invoke(() => Results.Ok(_workflow.Scan(id.ToString()))));
        app.MapGet("/projects/{id:guid}/fields", (Guid id) => Invoke(() => Results.Ok(_workflow.GetFields(id.ToString()))));
        app.MapGet("/projects/{id:guid}/mapping", (Guid id) => Invoke(() => Results.Ok(_workflow.GetMappings(id.ToString()))));
        app.MapPost("/projects/{id:guid}/mapping", (Guid id, MappingUpdateRequest request) => Invoke(() => Results.Ok(_workflow.UpdateMapping(id.ToString(), request))));
        app.MapPost("/projects/{id:guid}/template", (Guid id, TemplateUpdateRequest request) => Invoke(() => Results.Ok(_workflow.SetTemplate(id.ToString(), request))));
        app.MapPost("/projects/{id:guid}/preflight", (Guid id, PreflightRequest request) => Invoke(() => Results.Ok(_workflow.Preflight(id.ToString(), request.Mode))));
        app.MapPost("/projects/{id:guid}/generate", (Guid id, GenerateRequest request) => Invoke(() => { var result = _workflow.Generate(id.ToString(), request.Mode); return result.Success ? Results.Ok(result) : Results.BadRequest(result); }));
        app.MapGet("/wps/{fileName}", (string fileName) => ServeAddinFile(fileName));
    }

    private IResult ServeAddinFile(string fileName)
    {
        var allowed = fileName is "ribbon.xml" or "index.html" or "main.js" or "manifest.xml" or "taskpane.html" or "taskpane.js";
        var path = Path.Combine(_options.AddinRoot, fileName);
        if (!allowed || !File.Exists(path)) return Results.NotFound();
        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".js" => "text/javascript; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            _ => "text/html; charset=utf-8"
        };
        return Results.File(path, contentType);
    }
    private static IResult Invoke(Func<IResult> action)
    {
        try { return action(); }
        catch (UnknownProjectException ex) { return Results.NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (IOException ex) { return Results.Conflict(new { error = ex.Message }); }
    }
    public async ValueTask DisposeAsync() => await StopAsync();
}
