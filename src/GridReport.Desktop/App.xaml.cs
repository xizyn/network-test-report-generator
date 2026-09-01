using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using GridReport.Bridge;
using Serilog;

namespace GridReport.Desktop;

public partial class App : Application
{
    public BridgeHost? Bridge { get; private set; }
    public string BridgeStatus { get; private set; } = "未启动";
    public string BridgeAddress => Bridge?.Address ?? $"http://127.0.0.1:{BridgeOptions.DefaultPort}";

    protected override void OnStartup(StartupEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridReport", "logs", "app-.log");
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30).CreateLogger();
        DispatcherUnhandledException += (_, args) => { Log.Error(args.Exception, "未处理 UI 异常"); MessageBox.Show("发生未处理异常，详情已记录到日志。", "涉网试验报告", MessageBoxButton.OK, MessageBoxImage.Error); args.Handled = true; };
        StartBridge();
        base.OnStartup(e);
    }

    public void StartBridge()
    {
        try
        {
            Bridge = new BridgeHost(new BridgeOptions { AddinRoot = Path.Combine(AppContext.BaseDirectory, "wps-addin") });
            Bridge.StartAsync().GetAwaiter().GetResult();
            BridgeStatus = "运行中（WPS 请求：未检测到）";
            Log.Information("WPS Bridge 已启动于 {Address}", Bridge.Address);
        }
        catch (Exception ex)
        {
            Bridge = null; BridgeStatus = $"未启动：{ex.Message}";
            Log.Error(ex, "WPS Bridge 启动失败");
        }
    }

    public void RestartBridge()
    {
        try { Bridge?.StopAsync().GetAwaiter().GetResult(); } catch (Exception ex) { Log.Warning(ex, "停止 WPS Bridge 时发生异常"); }
        StartBridge();
    }

    public void RegisterBridgeProject(string projectFile)
    {
        try { Bridge?.RegisterProject(projectFile); } catch (Exception ex) { Log.Warning(ex, "无法向 WPS Bridge 登记项目 {ProjectFile}", projectFile); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { Bridge?.StopAsync().GetAwaiter().GetResult(); } catch (Exception ex) { Log.Warning(ex, "WPS Bridge 停止失败"); }
        Log.CloseAndFlush(); base.OnExit(e);
    }
}

