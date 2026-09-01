using System.Diagnostics;
using System.IO;
using System.Windows;
using GridReport.Core.Validation;
using GridReport.Desktop.ViewModels;
using Microsoft.Win32;

namespace GridReport.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    public MainWindow() { InitializeComponent(); DataContext = _viewModel; }

    private void NewProject_Click(object sender, RoutedEventArgs e) => _viewModel.NewProject("新建涉网试验项目", string.Empty, string.Empty, string.Empty, DateTime.Today);
    private void ChooseSource_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFolderDialog(); if (dialog.ShowDialog() == true) _viewModel.Project.SourceFolder = dialog.FolderName; }
    private void ChooseOutput_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFolderDialog(); if (dialog.ShowDialog() == true) _viewModel.Project.OutputFolder = dialog.FolderName; }
    private void ChooseTemplate_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = "Word 模板 (*.docx)|*.docx" }; if (dialog.ShowDialog() == true) _viewModel.Project.TemplatePath = dialog.FileName; }
    private void Scan_Click(object sender, RoutedEventArgs e) => Run(() => _viewModel.ScanFolder(Required(_viewModel.Project.SourceFolder, "请选择客户资料目录。")));
    private void LoadTemplate_Click(object sender, RoutedEventArgs e) => Run(() => _viewModel.LoadTemplate(Required(_viewModel.Project.TemplatePath, "请选择 DOCX 报告模板。")));
    private void ReMatch_Click(object sender, RoutedEventArgs e) => Run(_viewModel.ReMatch);
    private void CheckFormal_Click(object sender, RoutedEventArgs e) => Run(() => _viewModel.Check(DefaultOutputPath(), ReportMode.Formal));
    private void GenerateDraft_Click(object sender, RoutedEventArgs e) => Generate(ReportMode.Draft);
    private void GenerateFormal_Click(object sender, RoutedEventArgs e) => Generate(ReportMode.Formal);
    private void OpenOutput_Click(object sender, RoutedEventArgs e) => Run(() => Process.Start(new ProcessStartInfo("explorer.exe", Required(_viewModel.Project.OutputFolder, "请先设置输出目录。")) { UseShellExecute = true }));
    private void SaveProject_Click(object sender, RoutedEventArgs e) { var dialog = new SaveFileDialog { Filter = "涉网试验项目 (*.gridreport.json)|*.gridreport.json", FileName = _viewModel.Project.Name + ".gridreport.json" }; if (dialog.ShowDialog() == true) Run(() => _viewModel.Save(dialog.FileName)); }
    private void OpenProject_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = "涉网试验项目 (*.gridreport.json)|*.gridreport.json" }; if (dialog.ShowDialog() == true) Run(() => _viewModel.Open(dialog.FileName)); }
    private void RestartBridge_Click(object sender, RoutedEventArgs e) => Run(_viewModel.RestartBridge);

    private void Generate(ReportMode mode)
    {
        var dialog = new SaveFileDialog { Filter = "Word 文档 (*.docx)|*.docx", InitialDirectory = _viewModel.Project.OutputFolder, FileName = $"{DateTime.Today:yyyyMMdd}_{_viewModel.Project.Name}_涉网试验报告.docx", OverwritePrompt = false };
        if (dialog.ShowDialog() == true) Run(() => _viewModel.Generate(dialog.FileName, mode));
    }
    private string DefaultOutputPath() => Path.Combine(Required(_viewModel.Project.OutputFolder, "请先设置输出目录。"), $"{DateTime.Today:yyyyMMdd}_{_viewModel.Project.Name}_涉网试验报告.docx");
    private static string Required(string? value, string message) => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException(message);
    private void Run(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(ex.Message, "涉网试验报告", MessageBoxButton.OK, MessageBoxImage.Warning); } }
}
