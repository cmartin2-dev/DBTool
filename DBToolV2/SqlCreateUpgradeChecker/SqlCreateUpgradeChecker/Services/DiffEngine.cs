using System.Text.RegularExpressions;

namespace SqlCreateUpgradeChecker.Services;

/// <summary>
/// Represents a single SQL statement block split by GO delimiter.
/// </summary>
public class ScriptBlock
{
    public int Index { get; set; }
    public string RawText { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Normalized { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The SQL object name extracted from the statement (e.g., table name, trigger name).
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;
}

public class DiffResult
{
    public int Index { get; set; }
    public int LineA { get; set; }
    public int LineB { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ContentA { get; set; } = string.Empty;
    public string ContentB { get; set; } = string.Empty;
    public string ScriptId { get; set; } = string.Empty;

    public override string ToString() =>
        Type switch
        {
            "Missing" => $"[MISSING] A line {LineA}: {ScriptId}",
            "Modified" => $"[MODIFIED] A:{LineA} / B:{LineB}: {ScriptId}",
            "Present" => $"[OK] A:{LineA} / B:{LineB}: {ScriptId}",
            _ => ScriptId
        };
}

public static class DiffEngine
{
    /// <summary>
    /// Splits a SQL file into individual statement blocks using GO as delimiter.
    /// Skips empty blocks and Liquibase metadata lines.
    /// </summary>
    public static List<ScriptBlock> ParseStatements(string content)
    {
        var blocks = new List<ScriptBlock>();
        var lines = content.Replace("\r", "").Split('\n');

        var currentLines = new List<string>();
        int startLine = 1;
        int blockIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            if (string.Equals(trimmed, "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentLines.Count > 0)
                {
                    var raw = string.Join("\n", currentLines);
                    var normalized = NormalizeSql(raw);

                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        blocks.Add(new ScriptBlock
                        {
                            Index = ++blockIndex,
                            RawText = raw,
                            StartLine = startLine,
                            EndLine = i + 1,
                            Normalized = normalized,
                            Label = ExtractLabel(raw),
                            ObjectName = ExtractObjectName(raw)
                        });
                    }
                }

                currentLines.Clear();
                startLine = i + 2; // next line after GO (1-based)
            }
            else
            {
                if (currentLines.Count == 0)
                    startLine = i + 1;
                currentLines.Add(lines[i]);
            }
        }

        // Handle trailing block without final GO
        if (currentLines.Count > 0)
        {
            var raw = string.Join("\n", currentLines);
            var normalized = NormalizeSql(raw);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                blocks.Add(new ScriptBlock
                {
                    Index = ++blockIndex,
                    RawText = raw,
                    StartLine = startLine,
                    EndLine = lines.Length,
                    Normalized = normalized,
                    Label = ExtractLabel(raw),
                    ObjectName = ExtractObjectName(raw)
                });
            }
        }

        return blocks;
    }

    /// <summary>
    /// Normalizes SQL by stripping Liquibase comments, whitespace, and casing.
    /// </summary>
    private static string NormalizeSql(string sql)
    {
        var lines = sql.Replace("\r", "").Split('\n');
        var filtered = lines
            .Select(l => l.Trim())
            .Where(l =>
                !string.IsNullOrEmpty(l)
                && !l.StartsWith("--")  // Strip ALL SQL comments
                && !l.Equals("GO", StringComparison.OrdinalIgnoreCase)
            );

        var joined = string.Join(" ", filtered);

        // Collapse multiple whitespace
        joined = Regex.Replace(joined, @"\s+", " ").Trim();

        // Case-insensitive comparison: normalize to upper
        return joined.ToUpperInvariant();
    }

    /// <summary>
    /// Extracts a short label from the SQL statement for display.
    /// </summary>
    private static string ExtractLabel(string sql)
    {
        var lines = sql.Split('\n');
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (string.IsNullOrEmpty(t) || t.StartsWith("--") || t.Equals("GO", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = Regex.Match(t,
                @"(CREATE\s+TABLE|ALTER\s+TABLE|CREATE\s+(?:UNIQUE\s+)?(?:CLUSTERED\s+|NONCLUSTERED\s+)?INDEX|CREATE\s+OR\s+ALTER\s+(?:PROC|PROCEDURE|FUNCTION|VIEW|TRIGGER)|CREATE\s+(?:PROC|PROCEDURE|FUNCTION|VIEW|TRIGGER)|ALTER\s+INDEX|DROP\s+INDEX|INSERT|UPDATE|DELETE|EXEC|IF\s+NOT\s+EXISTS|IF\s+EXISTS)\b",
                RegexOptions.IgnoreCase);

            if (match.Success)
                return t.Length > 120 ? t[..120] + "..." : t;

            return t.Length > 120 ? t[..120] + "..." : t;
        }

        return "(empty)";
    }

    /// <summary>
    /// Extracts the SQL object name (table, trigger, proc, view, function, index name).
    /// </summary>
    private static string ExtractObjectName(string sql)
    {
        var normalized = sql.Replace("\r", "");
        
        // Match patterns like: CREATE OR ALTER TRIGGER [schema].[NAME]
        // or: ALTER TABLE [schema].[NAME]
        // or: CREATE TABLE [schema].[NAME]
        // or: CREATE INDEX [NAME] ON [schema].[TABLE]
        var patterns = new[]
        {
            @"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+(?:TRIGGER|PROC(?:EDURE)?|FUNCTION|VIEW)\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            @"(?:CREATE|ALTER)\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            @"CREATE\s+(?:UNIQUE\s+)?(?:CLUSTERED\s+|NONCLUSTERED\s+)?INDEX\s+[\[\s]*(\w+)[\]\s]*",
            @"EXEC\s+sp_settriggerorder\s+@triggername\s*=\s*N?'[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*'",
            @"(?:ENABLE|DISABLE)\s+TRIGGER\s+[\[\s]*(\w+)[\]\s]*",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(normalized, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
                return match.Groups[1].Value.ToUpperInvariant();
        }

        return string.Empty;
    }

    /// <summary>
    /// One-directional check: for each SQL statement in A, check if it exists in B.
    /// 1. Exact content match -> Present
    /// 2. Same object name but different content -> Modified
    /// 3. Not found at all -> Missing
    /// </summary>
    public static List<DiffResult> ComputeScriptDiff(string textA, string textB)
    {
        var blocksA = ParseStatements(textA);
        var blocksB = ParseStatements(textB);

        var diffs = new List<DiffResult>();
        int diffIndex = 1;

        // Index B by normalized content
        var bByContent = new Dictionary<string, ScriptBlock>();
        foreach (var b in blocksB)
            bByContent.TryAdd(b.Normalized, b);

        // Index B by object name (collect all blocks per name)
        var bByName = new Dictionary<string, List<ScriptBlock>>();
        foreach (var b in blocksB)
        {
            if (string.IsNullOrEmpty(b.ObjectName)) continue;
            if (!bByName.ContainsKey(b.ObjectName))
                bByName[b.ObjectName] = [];
            bByName[b.ObjectName].Add(b);
        }

        foreach (var blockA in blocksA)
        {
            // 1. Exact content match
            if (bByContent.ContainsKey(blockA.Normalized))
            {
                var matchB = bByContent[blockA.Normalized];
                diffs.Add(new DiffResult
                {
                    Index = diffIndex++,
                    LineA = blockA.StartLine,
                    LineB = matchB.StartLine,
                    Type = "Present",
                    ContentA = blockA.RawText.TrimEnd(),
                    ContentB = matchB.RawText.TrimEnd(),
                    ScriptId = blockA.Label
                });
            }
            // 2. Name exists in B but content differs
            else if (!string.IsNullOrEmpty(blockA.ObjectName) && bByName.ContainsKey(blockA.ObjectName))
            {
                var matchB = bByName[blockA.ObjectName][0];
                diffs.Add(new DiffResult
                {
                    Index = diffIndex++,
                    LineA = blockA.StartLine,
                    LineB = matchB.StartLine,
                    Type = "Modified",
                    ContentA = blockA.RawText.TrimEnd(),
                    ContentB = matchB.RawText.TrimEnd(),
                    ScriptId = blockA.Label
                });
            }
            // 3. Not found
            else
            {
                diffs.Add(new DiffResult
                {
                    Index = diffIndex++,
                    LineA = blockA.StartLine,
                    LineB = 0,
                    Type = "Missing",
                    ContentA = blockA.RawText.TrimEnd(),
                    ContentB = string.Empty,
                    ScriptId = blockA.Label
                });
            }
        }

        return diffs;
    }

    /// <summary>
    /// For backward compat with the consistency check tab.
    /// </summary>
    public static List<ScriptBlock> ParseScriptBlocks(string content) => ParseStatements(content);

    /// <summary>
    /// Adds line numbers to each line of text.
    /// </summary>
    public static string AddLineNumbers(string text)
    {
        var lines = text.Split('\n');
        var width = lines.Length.ToString().Length;
        return string.Join("\n",
            lines.Select((line, i) => $"{(i + 1).ToString().PadLeft(width)}  {line.TrimEnd()}"));
    }
}
