import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const [outputPath, previewPath] = process.argv.slice(2);
if (!outputPath) throw new Error("usage: New-WpsFriendTestData.mjs <output.xlsx> [preview.png]");

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("测试数据");
sheet.showGridLines = false;
sheet.getRange("A1:B1").merge();
sheet.getRange("A1").values = [["涉网试验报告系统 - WPS 集成测试数据"]];
sheet.getRange("A3:B8").values = [
  ["字段", "测试值"],
  ["项目名称", "WPS集成测试光伏项目"],
  ["客户名称", "测试新能源有限公司"],
  ["逆变器型号", "TEST-100KTL"],
  ["装机容量", "1MW"],
  ["测试日期", "2026-09-02"]
];
sheet.getRange("A1:B1").format = { fill: "#12365B", font: { bold: true, color: "#FFFFFF", size: 14 }, horizontalAlignment: "center", verticalAlignment: "center" };
sheet.getRange("A3:B3").format = { fill: "#DCEAF7", font: { bold: true, color: "#12365B" }, horizontalAlignment: "center", borders: { preset: "outside", style: "thin", color: "#AAB9C8" } };
sheet.getRange("A4:B8").format = { borders: { preset: "inside", style: "thin", color: "#D9E1E8" }, verticalAlignment: "center" };
sheet.getRange("A3:B8").format.columnWidth = 22;
sheet.getRange("B3:B8").format.columnWidth = 32;
sheet.getRange("A1").format.rowHeight = 28;
sheet.getRange("A3:B8").format.rowHeight = 22;
sheet.freezePanes.freezeRows(3);

const check = await workbook.inspect({ kind: "table", range: "测试数据!A1:B8", include: "values", tableMaxRows: 8, tableMaxCols: 2 });
if (!check.ndjson.includes("WPS集成测试光伏项目")) throw new Error("测试数据写入验证失败");
if (previewPath) {
  const preview = await workbook.render({ sheetName: "测试数据", range: "A1:B8", scale: 2, format: "png" });
  await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
}
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
