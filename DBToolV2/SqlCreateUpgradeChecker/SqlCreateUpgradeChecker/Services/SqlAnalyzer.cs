using System.IO;
using System.Text.RegularExpressions;
using SqlCreateUpgradeChecker.Models;

namespace SqlCreateUpgradeChecker.Services;

public class SqlAnalyzer
{
    private readonly string _rootFolder;

    public SqlAnalyzer(string rootFolder)
    {
        _rootFolder = rootFolder;
    }

    public List<string> GetVersions()
    {
        var createDir = Path.Combine(_rootFolder, "CREATE");
        var upgradeDir = Path.Combine(_rootFolder, "UPGRADE");

        if (!Directory.Exists(createDir) || !Directory.Exists(upgradeDir))
            return [];

        var createVersions = Directory.GetDirectories(createDir).Select(Path.GetFileName).ToHashSet();
        var upgradeVersions = Directory.GetDirectories(upgradeDir).Select(Path.GetFileName).ToHashSet();

        return createVersions.Intersect(upgradeVersions)
            .Where(v => v != null)
            .Cast<string>()
            .OrderBy(v => v)
            .ToList();
    }

    public List<AnalysisResult> Analyze(string version, string schema)
    {
        var results = new List<AnalysisResult>();

        var upgradeDir = Path.Combine(_rootFolder, "UPGRADE", version, schema);
        var createDir = Path.Combine(_rootFolder, "CREATE", version, schema);

        if (!Directory.Exists(upgradeDir) || !Directory.Exists(createDir))
            return results;

        var upgradeFiles = Directory.GetFiles(upgradeDir, "*.sql");

        foreach (var upgradeFile in upgradeFiles)
        {
            var fileName = Path.GetFileName(upgradeFile);
            var fileType = GetFileType(fileName, schema);
            var upgradeContent = File.ReadAllText(upgradeFile);
            var changesets = ParseChangesets(upgradeContent);

            foreach (var changeset in changesets)
            {
                var changes = ExtractChanges(changeset, fileType);
                foreach (var change in changes)
                {
                    change.UpgradeScript = changeset.FullText;
                    change.UpgradeFileName = fileName;
                    CheckAgainstCreate(change, createDir, schema);
                    results.Add(change);
                }
            }
        }

        return results;
    }

    private static string GetFileType(string fileName, string schema)
    {
        var upper = fileName.ToUpperInvariant();
        var prefix = schema.ToUpperInvariant() + "_UPGRADE_";
        var updatePrefix = schema.ToUpperInvariant() + "_UPDATE_";

        if (upper.Contains(prefix + "TABLES")) return "Tables";
        if (upper.Contains(prefix + "VIEWS")) return "Views";
        if (upper.Contains(prefix + "FUNCTIONS")) return "Functions";
        if (upper.Contains(prefix + "STORED_PROCEDURES")) return "Stored Procedures";
        if (upper.Contains(prefix + "TRIGGERS")) return "Triggers";
        if (upper.Contains(prefix + "INDEXES")) return "Indexes";
        if (upper.Contains(prefix + "CONSTRAINTS")) return "Constraints";
        if (upper.Contains(prefix + "METADATA") || upper.Contains(prefix + "META_DATA")) return "Metadata";
        if (upper.Contains(prefix + "PRE_METADATA")) return "Pre-Metadata";
        if (upper.Contains(prefix + "POST_METADATA")) return "Post-Metadata";
        if (upper.Contains(prefix + "LAYOUT_METADATA")) return "Layout Metadata";
        if (upper.Contains(prefix + "ROLE_VIEW")) return "Role View";
        if (upper.Contains(updatePrefix)) return "Feature Toggle";
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static List<ChangesetInfo> ParseChangesets(string content)
    {
        var changesets = new List<ChangesetInfo>();
        var lines = content.Split('\n');
        ChangesetInfo? current = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("--changeset ", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null && !string.IsNullOrWhiteSpace(current.Body))
                    changesets.Add(current);

                current = new ChangesetInfo
                {
                    Id = ExtractChangesetId(trimmed),
                    FullText = line + "\n"
                };
            }
            else if (current != null)
            {
                current.FullText += line + "\n";

                if (!trimmed.StartsWith("--param ", StringComparison.OrdinalIgnoreCase) &&
                    !trimmed.StartsWith("--comment", StringComparison.OrdinalIgnoreCase) &&
                    trimmed != "--liquibase formatted sql")
                {
                    current.Body += line + "\n";
                }
            }
        }

        if (current != null && !string.IsNullOrWhiteSpace(current.Body))
            changesets.Add(current);

        return changesets;
    }

    private static string ExtractChangesetId(string line)
    {
        // --changeset author:id splitStatements...
        var match = Regex.Match(line, @"--changeset\s+(\S+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private static List<AnalysisResult> ExtractChanges(ChangesetInfo changeset, string fileType)
    {
        var results = new List<AnalysisResult>();
        var body = changeset.Body;

        // Detect CREATE TABLE
        var createTableMatches = Regex.Matches(body,
            @"CREATE\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.[\[\s]*(\w+)[\]\s]*\s*\(",
            RegexOptions.IgnoreCase);
        foreach (Match m in createTableMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "New Table",
                ObjectName = m.Groups[1].Value,
                Detail = "Full table creation in UPGRADE",
                ChangesetId = changeset.Id
            });
        }

        // Detect ALTER TABLE ... ADD (new columns)
        var addColumnMatches = Regex.Matches(body,
            @"ALTER\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*\s+ADD\s+[\[\s]*(\w+)[\]\s]*\s+(\w[\w\(\),\s]*?)(?:\s+(?:NOT\s+)?NULL|;|\s+DEFAULT|\s+CONSTRAINT)",
            RegexOptions.IgnoreCase);
        foreach (Match m in addColumnMatches)
        {
            var tableName = m.Groups[1].Value;
            var columnName = m.Groups[2].Value;
            var dataType = m.Groups[3].Value.Trim();

            // Skip if this is a constraint addition, not a column
            if (columnName.Equals("CONSTRAINT", StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "New Column",
                ObjectName = tableName,
                Detail = $"Column: {columnName} ({dataType})",
                ChangesetId = changeset.Id
            });
        }

        // Detect ALTER TABLE ... ALTER COLUMN (type changes)
        var alterColumnMatches = Regex.Matches(body,
            @"ALTER\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*\s+ALTER\s+COLUMN\s+[\[\s]*(\w+)[\]\s]*\s+(\w[\w\(\),\s]*?)(?:\s+(?:NOT\s+)?NULL|;|\s*$)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        foreach (Match m in alterColumnMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Alter Column",
                ObjectName = m.Groups[1].Value,
                Detail = $"Column: {m.Groups[2].Value} -> {m.Groups[3].Value.Trim()}",
                ChangesetId = changeset.Id
            });
        }

        // Detect ALTER TABLE ... DROP COLUMN
        var dropColumnMatches = Regex.Matches(body,
            @"ALTER\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*\s+DROP\s+COLUMN\s+[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in dropColumnMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Drop Column",
                ObjectName = m.Groups[1].Value,
                Detail = $"Column: {m.Groups[2].Value}",
                ChangesetId = changeset.Id
            });
        }

        // Detect CREATE INDEX
        var createIndexMatches = Regex.Matches(body,
            @"CREATE\s+(?:UNIQUE\s+)?(?:CLUSTERED\s+|NONCLUSTERED\s+)?INDEX\s+[\[\s]*(\w+)[\]\s]*\s+ON\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in createIndexMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "New Index",
                ObjectName = m.Groups[2].Value,
                Detail = $"Index: {m.Groups[1].Value}",
                ChangesetId = changeset.Id
            });
        }

        // Detect CREATE OR ALTER / ALTER PROCEDURE
        var procMatches = Regex.Matches(body,
            @"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+PROC(?:EDURE)?\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in procMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Stored Procedure",
                ObjectName = m.Groups[1].Value,
                Detail = "Procedure definition in UPGRADE",
                ChangesetId = changeset.Id
            });
        }

        // Detect CREATE OR ALTER / ALTER FUNCTION
        var funcMatches = Regex.Matches(body,
            @"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+FUNCTION\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in funcMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Function",
                ObjectName = m.Groups[1].Value,
                Detail = "Function definition in UPGRADE",
                ChangesetId = changeset.Id
            });
        }

        // Detect CREATE OR ALTER / ALTER VIEW
        var viewMatches = Regex.Matches(body,
            @"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+VIEW\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in viewMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "View",
                ObjectName = m.Groups[1].Value,
                Detail = "View definition in UPGRADE",
                ChangesetId = changeset.Id
            });
        }

        // Detect CREATE OR ALTER / ALTER TRIGGER
        var triggerMatches = Regex.Matches(body,
            @"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+TRIGGER\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in triggerMatches)
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Trigger",
                ObjectName = m.Groups[1].Value,
                Detail = "Trigger definition in UPGRADE",
                ChangesetId = changeset.Id
            });
        }

        // Detect ADD CONSTRAINT
        var constraintMatches = Regex.Matches(body,
            @"ADD\s+CONSTRAINT\s+[\[\s]*(\w+)[\]\s]*",
            RegexOptions.IgnoreCase);
        foreach (Match m in constraintMatches)
        {
            // Try to find the table name from context
            var tableMatch = Regex.Match(body,
                @"ALTER\s+TABLE\s+[\[\s]*(?:\$\{schemaName\})?[\]\s]*\.?[\[\s]*(\w+)[\]\s]*",
                RegexOptions.IgnoreCase);
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "New Constraint",
                ObjectName = tableMatch.Success ? tableMatch.Groups[1].Value : "Unknown",
                Detail = $"Constraint: {m.Groups[1].Value}",
                ChangesetId = changeset.Id
            });
        }

        // If no specific changes detected but body has real SQL, flag as unrecognized
        if (results.Count == 0 && HasRealSql(body))
        {
            results.Add(new AnalysisResult
            {
                FileType = fileType,
                ChangeType = "Other/Script",
                ObjectName = "—",
                Detail = GetFirstMeaningfulLine(body),
                ChangesetId = changeset.Id
            });
        }

        return results;
    }

    private static bool HasRealSql(string body)
    {
        var stripped = Regex.Replace(body, @"--.*$", "", RegexOptions.Multiline).Trim();
        stripped = stripped.Replace("GO", "").Replace("\r", "").Replace("\n", "").Trim();
        return stripped.Length > 5;
    }

    private static string GetFirstMeaningfulLine(string body)
    {
        var lines = body.Split('\n');
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (!string.IsNullOrEmpty(t) && !t.StartsWith("--") && t != "GO")
                return t.Length > 120 ? t[..120] + "..." : t;
        }
        return "Non-empty changeset body";
    }

    private static void CheckAgainstCreate(AnalysisResult result, string createDir, string schema)
    {
        var createFiles = Directory.GetFiles(createDir, "*.sql");
        var fileContents = createFiles.ToDictionary(f => Path.GetFileName(f), File.ReadAllText);
        var allCreateContent = string.Join("\n", fileContents.Values);

        switch (result.ChangeType)
        {
            case "New Table":
                CheckNewTable(result, allCreateContent);
                break;
            case "New Column":
                CheckNewColumn(result, allCreateContent);
                break;
            case "Alter Column":
                CheckAlterColumn(result, allCreateContent);
                break;
            case "Drop Column":
                CheckDropColumn(result, allCreateContent);
                break;
            case "New Index":
                CheckNewIndex(result, allCreateContent);
                break;
            case "Stored Procedure":
                CheckNamedObject(result, allCreateContent, "PROC");
                break;
            case "Function":
                CheckNamedObject(result, allCreateContent, "FUNCTION");
                break;
            case "View":
                CheckNamedObject(result, allCreateContent, "VIEW");
                break;
            case "Trigger":
                CheckNamedObject(result, allCreateContent, "TRIGGER");
                break;
            case "New Constraint":
                CheckConstraint(result, allCreateContent);
                break;
            default:
                result.Status = "Needs Manual Review";
                break;
        }

        // Find the CREATE file that contains this object and extract a snippet
        FindCreateSnippet(result, fileContents);
    }

    private static void FindCreateSnippet(AnalysisResult result, Dictionary<string, string> fileContents)
    {
        var objectName = result.ObjectName;
        if (string.IsNullOrEmpty(objectName) || objectName == "—") return;

        foreach (var (fileName, content) in fileContents)
        {
            var pattern = $@"\[{Regex.Escape(objectName)}\]";
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            result.CreateFileName = fileName;

            // Extract surrounding context (up to 30 lines around the match)
            var lines = content.Split('\n');
            var matchPos = match.Index;
            var charCount = 0;
            var matchLine = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                charCount += lines[i].Length + 1;
                if (charCount >= matchPos)
                {
                    matchLine = i;
                    break;
                }
            }

            var startLine = Math.Max(0, matchLine - 5);
            var endLine = Math.Min(lines.Length - 1, matchLine + 25);
            result.CreateSnippet = string.Join("\n", lines[startLine..(endLine + 1)]);
            return;
        }
    }

    private static void CheckNewTable(AnalysisResult result, string createContent)
    {
        var pattern = $@"CREATE\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.[\[\s]*{Regex.Escape(result.ObjectName)}[\]\s]*\s*\(";
        var match = Regex.IsMatch(createContent, pattern, RegexOptions.IgnoreCase);

        if (match)
        {
            // Check if it's in a CREATE TABLE (proper) or in an IF NOT EXISTS / ALTER block
            var ifExistsPattern = $@"IF\s+NOT\s+EXISTS[\s\S]*?CREATE\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.[\[\s]*{Regex.Escape(result.ObjectName)}[\]\s]*\s*\(";
            if (Regex.IsMatch(createContent, ifExistsPattern, RegexOptions.IgnoreCase))
                result.Status = "Appended as ALTER";
            else
                result.Status = "Present";
        }
        else
        {
            result.Status = "Missing";
        }
    }

    private static void CheckNewColumn(AnalysisResult result, string createContent)
    {
        // Extract column name from detail
        var colMatch = Regex.Match(result.Detail, @"Column:\s*(\w+)");
        if (!colMatch.Success) { result.Status = "Parse Error"; return; }

        var columnName = colMatch.Groups[1].Value;
        var tableName = result.ObjectName;

        // Check if column exists in a CREATE TABLE definition
        var tablePattern = $@"CREATE\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.[\[\s]*{Regex.Escape(tableName)}[\]\s]*\s*\((?<body>[^;]*?)\)\s*ON\s+\[PRIMARY\]";
        var tableMatch = Regex.Match(createContent, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (tableMatch.Success)
        {
            var tableBody = tableMatch.Groups["body"].Value;
            var colPattern = $@"\[{Regex.Escape(columnName)}\]";
            if (Regex.IsMatch(tableBody, colPattern, RegexOptions.IgnoreCase))
            {
                result.Status = "Present";
                return;
            }
        }

        // Check if it's appended as ALTER TABLE ADD
        var alterPattern = $@"ALTER\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.?[\[\s]*{Regex.Escape(tableName)}[\]\s]*\s+ADD\s+[\[\s]*{Regex.Escape(columnName)}[\]\s]*";
        if (Regex.IsMatch(createContent, alterPattern, RegexOptions.IgnoreCase))
        {
            result.Status = "Appended as ALTER";
            return;
        }

        result.Status = "Missing";
    }

    private static void CheckAlterColumn(AnalysisResult result, string createContent)
    {
        var colMatch = Regex.Match(result.Detail, @"Column:\s*(\w+)\s*->\s*(.+)");
        if (!colMatch.Success) { result.Status = "Parse Error"; return; }

        var columnName = colMatch.Groups[1].Value;
        var newType = colMatch.Groups[2].Value.Trim();
        var tableName = result.ObjectName;

        // Check if the CREATE TABLE has the column with the correct type
        var tablePattern = $@"CREATE\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.[\[\s]*{Regex.Escape(tableName)}[\]\s]*\s*\((?<body>[\s\S]*?)\)\s*ON\s+\[PRIMARY\]";
        var tableMatch = Regex.Match(createContent, tablePattern, RegexOptions.IgnoreCase);

        if (tableMatch.Success)
        {
            var tableBody = tableMatch.Groups["body"].Value;
            var colPattern = $@"\[{Regex.Escape(columnName)}\]\s+\[?{Regex.Escape(newType)}";
            if (Regex.IsMatch(tableBody, colPattern, RegexOptions.IgnoreCase))
            {
                result.Status = "Present";
                return;
            }

            // Column exists but with old type
            var colExistsPattern = $@"\[{Regex.Escape(columnName)}\]";
            if (Regex.IsMatch(tableBody, colExistsPattern, RegexOptions.IgnoreCase))
            {
                // Check if there's an ALTER COLUMN appended
                var alterPattern = $@"ALTER\s+TABLE[\s\S]*?ALTER\s+COLUMN\s+[\[\s]*{Regex.Escape(columnName)}[\]\s]*\s+{Regex.Escape(newType)}";
                if (Regex.IsMatch(createContent, alterPattern, RegexOptions.IgnoreCase))
                {
                    result.Status = "Appended as ALTER";
                    return;
                }
                result.Status = "Wrong Type in CREATE";
                return;
            }
        }

        result.Status = "Missing";
    }

    private static void CheckDropColumn(AnalysisResult result, string createContent)
    {
        var colMatch = Regex.Match(result.Detail, @"Column:\s*(\w+)");
        if (!colMatch.Success) { result.Status = "Parse Error"; return; }

        var columnName = colMatch.Groups[1].Value;
        var tableName = result.ObjectName;

        // In a proper CREATE, the column should NOT exist
        var tablePattern = $@"CREATE\s+TABLE\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.[\[\s]*{Regex.Escape(tableName)}[\]\s]*\s*\((?<body>[\s\S]*?)\)\s*ON\s+\[PRIMARY\]";
        var tableMatch = Regex.Match(createContent, tablePattern, RegexOptions.IgnoreCase);

        if (tableMatch.Success)
        {
            var tableBody = tableMatch.Groups["body"].Value;
            var colPattern = $@"\[{Regex.Escape(columnName)}\]";
            if (Regex.IsMatch(tableBody, colPattern, RegexOptions.IgnoreCase))
            {
                result.Status = "Column Still in CREATE";
                return;
            }
        }

        result.Status = "Present";
    }

    private static void CheckNewIndex(AnalysisResult result, string createContent)
    {
        var idxMatch = Regex.Match(result.Detail, @"Index:\s*(\w+)");
        if (!idxMatch.Success) { result.Status = "Parse Error"; return; }

        var indexName = idxMatch.Groups[1].Value;
        var pattern = $@"\[{Regex.Escape(indexName)}\]";

        if (Regex.IsMatch(createContent, pattern, RegexOptions.IgnoreCase))
            result.Status = "Present";
        else
            result.Status = "Missing";
    }

    private static void CheckNamedObject(AnalysisResult result, string createContent, string objectType)
    {
        var pattern = $@"(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+{objectType}(?:EDURE)?\s+[\[\s]*(?:\$\{{schemaName\}})?[\]\s]*\.?[\[\s]*{Regex.Escape(result.ObjectName)}[\]\s]*";
        if (Regex.IsMatch(createContent, pattern, RegexOptions.IgnoreCase))
            result.Status = "Present";
        else
            result.Status = "Missing";
    }

    private static void CheckConstraint(AnalysisResult result, string createContent)
    {
        var cMatch = Regex.Match(result.Detail, @"Constraint:\s*(\w+)");
        if (!cMatch.Success) { result.Status = "Parse Error"; return; }

        var constraintName = cMatch.Groups[1].Value;
        var pattern = $@"\[{Regex.Escape(constraintName)}\]";

        if (Regex.IsMatch(createContent, pattern, RegexOptions.IgnoreCase))
            result.Status = "Present";
        else
            result.Status = "Missing";
    }

    private class ChangesetInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }

    public List<string> GetCreateFiles(string version, string schema)
    {
        var createDir = Path.Combine(_rootFolder, "CREATE", version, schema);
        if (!Directory.Exists(createDir))
            return [];

        return Directory.GetFiles(createDir, "*.sql")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderBy(f => f)
            .ToList();
    }

    public string GetCreateFileContent(string version, string schema, string fileName)
    {
        var filePath = Path.Combine(_rootFolder, "CREATE", version, schema, fileName);
        if (!File.Exists(filePath))
            return $"File not found: {filePath}";

        return File.ReadAllText(filePath);
    }
}
