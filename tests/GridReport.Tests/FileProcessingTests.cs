using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GridReport.Core.Domain;
using GridReport.Core.Services;

namespace GridReport.Tests;

public sealed class FileProcessingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "GridReportTests", Guid.NewGuid().ToString("N"));

    public FileProcessingTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void Detector_identifies_png_when_extension_is_dat()
    {
        var file = Path.Combine(_folder, "nameplate.dat");
        File.WriteAllBytes(file, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var result = new FileTypeDetector().Inspect(file);

        Assert.Equal(FileKind.Png, result.Kind);
        Assert.True(result.ExtensionMismatch);
    }

    [Fact]
    public void Detector_identifies_pdf_from_magic_number()
    {
        var file = Path.Combine(_folder, "manual.bin");
        File.WriteAllBytes(file, Encoding.ASCII.GetBytes("%PDF-1.7\n"));

        Assert.Equal(FileKind.Pdf, new FileTypeDetector().Inspect(file).Kind);
    }

    [Fact]
    public void Detector_marks_ooxml_extension_in_ole_container_as_possibly_encrypted()
    {
        var file = Path.Combine(_folder, "protected.xlsx");
        File.WriteAllBytes(file, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]);

        var result = new FileTypeDetector().Inspect(file);

        Assert.Equal(FileKind.OleCompound, result.Kind);
        Assert.True(result.IsPossiblyEncrypted);
    }

    [Fact]
    public void Scanner_continues_after_unreadable_or_unknown_file()
    {
        File.WriteAllText(Path.Combine(_folder, "data.csv"), "项目名称,XX光伏项目");
        File.WriteAllBytes(Path.Combine(_folder, "unknown.bin"), [1, 2, 3, 4]);

        var records = new CustomerPackageScanner(new FileTypeDetector()).Scan(_folder);

        Assert.Equal(2, records.Count);
        Assert.Contains(records, r => r.Kind == FileKind.Csv && r.Status == FileRecordStatus.Ready);
        Assert.Contains(records, r => r.Kind == FileKind.Unknown && r.Status == FileRecordStatus.NeedsReview);
    }

    [Fact]
    public void DelimitedExtractor_preserves_cell_provenance()
    {
        var file = Path.Combine(_folder, "customer.csv");
        File.WriteAllText(file, "客户名称,广州XX科技有限公司\n装机容量,5.8MW", new UTF8Encoding(true));

        var values = new DelimitedDataExtractor().Extract(file);

        Assert.Equal("广州XX科技有限公司", values.Single(x => x.Field == "客户名称").Value);
        Assert.Equal("A1:B1", values.Single(x => x.Field == "客户名称").Provenance.Location);
    }

    [Fact]
    public void ExcelExtractor_reads_key_value_rows_with_sheet_and_cell_provenance()
    {
        var file = Path.Combine(_folder, "customer.xlsx");
        using (var document = SpreadsheetDocument.Create(file, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart(); workbook.Workbook = new Workbook();
            var sheetPart = workbook.AddNewPart<WorksheetPart>(); sheetPart.Worksheet = new Worksheet(new SheetData(
                new Row(new Cell { CellReference = "A1", DataType = CellValues.InlineString, InlineString = new InlineString(new Text("装机容量")) }, new Cell { CellReference = "B1", DataType = CellValues.InlineString, InlineString = new InlineString(new Text("5.8MW")) })));
            var sheets = workbook.Workbook.AppendChild(new Sheets()); sheets.Append(new Sheet { Id = workbook.GetIdOfPart(sheetPart), SheetId = 1, Name = "项目参数" }); workbook.Workbook.Save(); sheetPart.Worksheet.Save();
        }

        var values = new ExcelDataExtractor().Extract(file);

        var value = Assert.Single(values);
        Assert.Equal("装机容量", value.Field);
        Assert.Equal("5.8MW", value.Value);
        Assert.Equal("项目参数!A1:B1", value.Provenance.Location);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
