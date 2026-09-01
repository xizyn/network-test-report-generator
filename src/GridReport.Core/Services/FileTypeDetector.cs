using System.IO.Compression;
using System.Text;

namespace GridReport.Core.Services;

public enum FileKind { Unknown, Docx, Xlsx, Xls, Pdf, Jpeg, Png, Bmp, Tiff, Csv, Txt, Dwg, Dxf, Fel, Zip, OleCompound }
public enum FileRecordStatus { Ready, NeedsReview, EncryptedOrProtected, Failed }
public sealed record FileInspection(FileKind Kind, bool ExtensionMismatch, string Detail, bool IsPossiblyEncrypted = false);

public sealed class FileTypeDetector
{
    public FileInspection Inspect(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[Math.Min(8192, (int)Math.Min(stream.Length, 8192))];
            _ = stream.Read(header, 0, header.Length);
            var kind = Detect(header, stream, extension);
            var expected = FromExtension(extension);
            var encrypted = kind == FileKind.OleCompound && extension is ".docx" or ".xlsx";
            return new FileInspection(kind, !string.IsNullOrWhiteSpace(extension) && kind != FileKind.Unknown && expected != kind, encrypted ? "疑似 OOXML 加密容器，需要密码或人工处理" : Describe(kind), encrypted);
        }
        catch (UnauthorizedAccessException ex) { return new(FileKind.Unknown, false, $"无法访问：{ex.Message}"); }
        catch (IOException ex) { return new(FileKind.Unknown, false, $"无法读取：{ex.Message}"); }
    }

    private static FileKind Detect(byte[] h, Stream stream, string extension)
    {
        if (Starts(h, 0x89, 0x50, 0x4E, 0x47)) return FileKind.Png;
        if (Starts(h, 0xFF, 0xD8, 0xFF)) return FileKind.Jpeg;
        if (Starts(h, 0x25, 0x50, 0x44, 0x46, 0x2D)) return FileKind.Pdf;
        if (Starts(h, 0x42, 0x4D)) return FileKind.Bmp;
        if (Starts(h, 0x49, 0x49, 0x2A, 0x00) || Starts(h, 0x4D, 0x4D, 0x00, 0x2A)) return FileKind.Tiff;
        if (Starts(h, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)) return extension is ".xls" ? FileKind.Xls : FileKind.OleCompound;
        if (Starts(h, 0x41, 0x43, 0x31, 0x30)) return FileKind.Dwg;
        if (Starts(h, 0x50, 0x4B, 0x03, 0x04) || Starts(h, 0x50, 0x4B, 0x05, 0x06)) return DetectZip(stream);
        var text = Encoding.UTF8.GetString(h).TrimStart('\uFEFF', '\0', ' ', '\r', '\n');
        if (text.StartsWith("0\nSECTION", StringComparison.Ordinal) || text.StartsWith("0\r\nSECTION", StringComparison.Ordinal)) return FileKind.Dxf;
        if (LooksLikeText(h)) return text.Contains(',') || text.Contains('\t') ? FileKind.Csv : FileKind.Txt;
        return extension == ".fel" ? FileKind.Fel : FileKind.Unknown;
    }

    private static FileKind DetectZip(Stream stream)
    {
        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            var entries = archive.Entries.Select(x => x.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (entries.Contains("word/document.xml")) return FileKind.Docx;
            if (entries.Contains("xl/workbook.xml")) return FileKind.Xlsx;
            return FileKind.Zip;
        }
        catch (InvalidDataException) { return FileKind.Zip; }
    }

    private static bool Starts(byte[] bytes, params byte[] expected) => bytes.Length >= expected.Length && bytes.Take(expected.Length).SequenceEqual(expected);
    private static bool LooksLikeText(byte[] bytes) => bytes.Length > 0 && bytes.Count(x => x == 0) == 0 && bytes.Count(x => x < 9 || (x > 13 && x < 32)) < Math.Max(2, bytes.Length / 20);
    private static FileKind FromExtension(string extension) => extension switch
    {
        ".docx" => FileKind.Docx, ".xlsx" => FileKind.Xlsx, ".xls" => FileKind.Xls, ".pdf" => FileKind.Pdf,
        ".jpg" or ".jpeg" => FileKind.Jpeg, ".png" => FileKind.Png, ".bmp" => FileKind.Bmp, ".tif" or ".tiff" => FileKind.Tiff,
        ".csv" => FileKind.Csv, ".txt" => FileKind.Txt, ".dwg" => FileKind.Dwg, ".dxf" => FileKind.Dxf, ".fel" => FileKind.Fel, _ => FileKind.Unknown
    };
    private static string Describe(FileKind kind) => kind switch
    {
        FileKind.Docx or FileKind.Xlsx => "OOXML ZIP 容器", FileKind.Fel => "福禄克数据文件（需官方软件导出）", FileKind.Dwg => "CAD 图纸（归档/转换工作流）", _ => kind.ToString()
    };
}
