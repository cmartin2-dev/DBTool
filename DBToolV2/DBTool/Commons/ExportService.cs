using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace DBTool.Commons;

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
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"{version} - {schema}");

        var headers = new[] { "File Type", "Change Type", "Object Name", "Detail", "Status", "Changeset ID" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        for (int row = 0; row < results.Count; row++)
        {
            var r = results[row];
            ws.Cell(row + 2, 1).Value = r.FileType;
            ws.Cell(row + 2, 2).Value = r.ChangeType;
            ws.Cell(row + 2, 3).Value = r.ObjectName;
            ws.Cell(row + 2, 4).Value = r.Detail;
            ws.Cell(row + 2, 5).Value = r.Status;
            ws.Cell(row + 2, 6).Value = r.ChangesetId;

            var statusCell = ws.Cell(row + 2, 5);
            switch (r.Status)
            {
                case "Missing":
                    statusCell.Style.Font.FontColor = XLColor.Red;
                    statusCell.Style.Font.Bold = true;
                    break;
                case "Appended as ALTER":
                case "Wrong Type in CREATE":
                case "Column Still in CREATE":
                    statusCell.Style.Font.FontColor = XLColor.DarkOrange;
                    statusCell.Style.Font.Bold = true;
                    break;
                case "Present":
                    statusCell.Style.Font.FontColor = XLColor.Green;
                    break;
            }
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
