using System.IO;
using System.Text;
using OfficeOpenXml;
using SqlCreateUpgradeChecker.Models;

namespace SqlCreateUpgradeChecker.Services;

public static class ExportService
{
    public static void ExportToCsv(List<AnalysisResult> results, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("File Type,Change Type,Object Name,Detail,Status,Changeset ID");

        foreach (var r in results)
        {
            sb.AppendLine($"{Escape(r.FileType)},{Escape(r.ChangeType)},{Escape(r.ObjectName)},{Escape(r.Detail)},{Escape(r.Status)},{Escape(r.ChangesetId)}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static void ExportToExcel(List<AnalysisResult> results, string filePath, string version, string schema)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add($"{version} - {schema}");

        // Headers
        var headers = new[] { "File Type", "Change Type", "Object Name", "Detail", "Status", "Changeset ID" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
        }

        // Data
        for (int row = 0; row < results.Count; row++)
        {
            var r = results[row];
            ws.Cells[row + 2, 1].Value = r.FileType;
            ws.Cells[row + 2, 2].Value = r.ChangeType;
            ws.Cells[row + 2, 3].Value = r.ObjectName;
            ws.Cells[row + 2, 4].Value = r.Detail;
            ws.Cells[row + 2, 5].Value = r.Status;
            ws.Cells[row + 2, 6].Value = r.ChangesetId;

            // Color-code status
            var statusCell = ws.Cells[row + 2, 5];
            switch (r.Status)
            {
                case "Missing":
                    statusCell.Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    statusCell.Style.Font.Bold = true;
                    break;
                case "Appended as ALTER":
                case "Wrong Type in CREATE":
                case "Column Still in CREATE":
                    statusCell.Style.Font.Color.SetColor(System.Drawing.Color.DarkOrange);
                    statusCell.Style.Font.Bold = true;
                    break;
                case "Present":
                    statusCell.Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    break;
            }
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        package.SaveAs(new FileInfo(filePath));
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
