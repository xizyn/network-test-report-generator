import fs from "node:fs/promises";

const [sourcePath, outputPath] = process.argv.slice(2);
if (!sourcePath || !outputPath) throw new Error("usage: Sanitize-WpsPublish.mjs <source.html> <output.html>");
const html = await fs.readFile(sourcePath, "utf8");
const addonList = 'var curList = [{"name":"gridreportwps","addonType":"wps","online":"true","multiUser":"false","url":"http://127.0.0.1:43801/wps/"}];';
const output = html.replace(/var curList = \[.*?\];(?=\s*curList\.forEach)/s, addonList);
if (!output.includes('"name":"gridreportwps"') || output.includes('"name":"test123"')) throw new Error("publish.html plugin list sanitization failed");
await fs.writeFile(outputPath, output, "utf8");
