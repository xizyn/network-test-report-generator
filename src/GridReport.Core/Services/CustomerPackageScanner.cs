namespace GridReport.Core.Services;

public sealed record FileRecord(string Path, string FileName, string Extension, FileKind Kind, FileRecordStatus Status, string Handling, string Detail);

public sealed class CustomerPackageScanner(FileTypeDetector detector)
{
    public List<FileRecord> Scan(string folder)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"资料目录不存在：{folder}");
        var records = new List<FileRecord>();
        foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            var inspection = detector.Inspect(path);
            var status = inspection.IsPossiblyEncrypted ? FileRecordStatus.EncryptedOrProtected : inspection.Kind switch
            {
                FileKind.Unknown or FileKind.Fel or FileKind.Dwg or FileKind.OleCompound => FileRecordStatus.NeedsReview,
                _ when inspection.Detail.StartsWith("无法", StringComparison.Ordinal) => FileRecordStatus.Failed,
                _ => FileRecordStatus.Ready
            };
            records.Add(new FileRecord(path, Path.GetFileName(path), Path.GetExtension(path), inspection.Kind, status, HandlingFor(inspection.Kind), inspection.Detail));
        }
        return records.OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string HandlingFor(FileKind kind) => kind switch
    {
        FileKind.Docx => "读取 Word/WPS 文本及批注", FileKind.Xlsx or FileKind.Xls => "读取工作表与单元格", FileKind.Csv or FileKind.Txt => "读取文本字段",
        FileKind.Pdf => "归档；文本型 PDF 可人工引用", FileKind.Jpeg or FileKind.Png or FileKind.Bmp or FileKind.Tiff => "纳入图片资源",
        FileKind.Dwg => "归档；请转换为 PDF/PNG 后插入", FileKind.Fel => "请使用 Energy Analyze Plus 导出 CSV/XLSX", _ => "需要人工确认"
    };
}
