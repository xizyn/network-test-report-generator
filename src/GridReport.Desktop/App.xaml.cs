using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

using Serilog;

namespace GridReport.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridReport", "logs", "app-.log");
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30).CreateLogger();
        DispatcherUnhandledException += (_, args) => { Log.Error(args.Exception, "未处理 UI 异常"); MessageBox.Show("发生未处理异常，详情已记录到日志。", "涉网试验报告", MessageBoxButton.OK, MessageBoxImage.Error); args.Handled = true; };
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e) { Log.CloseAndFlush(); base.OnExit(e); }
}

