using System.IO;
using System.Text.RegularExpressions;

namespace DBTool.Commons;

public class FixService
{
    private readonly string _rootFolder;

    // Maps UPGRADE file name patterns to CREATE file name patterns
    private static readonly Dictionary<string, string> UpgradeToCreateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "UPGRADE_TABLES", "CREATE_TABLES" },
        { "UPGRADE_VIEWS", "CREATE_VIEWS" },
        { "UPGRADE_FUNCTIONS", "CREATE_FUNCTIONS" },
        { "UPGRADE_STORED_PROCEDURES", "CREATE_STORED_PROCEDURES" },
        { "UPGRADE_TRIGGERS", "CREATE_TRIGGERS" },
        { "UPGRADE_INDEXES", "CREATE_CONSTRAINTS" },
        { "UPGRADE_CONSTRAINTS", "CREATE_CONSTRAINTS" },
        { "UPGRADE_METADATA", "CREATE_METADATA" },
        { "UPGRADE_META_DATA", "CREATE_METADATA" },
    };

    public FixService(string rootFolder)
    {
        _rootFolder = rootFolder;
    }

    public List<string> GetVersions()
    {
        var createDir = Path.Combine(_rootFolder, "CREATE");
        if (!Directory.Exists(createDir)) return [];

        return Directory.GetDirectories(createDir)
            .Select(Path.GetFileName)
            .Where(v => v != null)
            .Cast<string>()
            .OrderBy(v => v)
            .ToList();
    }

    public string? GetPreviousVersion(string version, List<string> versions)
    {
        var idx = versions.IndexOf(version);
        return idx > 0 ? versions[idx - 1] : null;
    }

    /// <summary>
    /// Finds statements missing from CREATE(version) that exist in CREATE(prevVersion).
    /// </summary>
    public List<FixItem> FindMissingFromPrevious(string version, string prevVersion, string schema)
    {
        var items = new List<FixItem>();
        var createDir = Path.Combine(_rootFolder, "CREATE", version, schema);
        var prevCreateDir = Path.Combine(_rootFolder, "CREATE", prevVersion, schema);

        if (!Directory.Exists(createDir) || !Directory.Exists(prevCreateDir))
            return items;

        var prevFiles = Directory.GetFiles(prevCreateDir, "*.sql");

        foreach (var prevFile in prevFiles)
        {
            var fileName = Path.GetFileName(prevFile);
            var currentFile = Path.Combine(createDir, fileName);

            if (!File.Exists(currentFile)) continue;

            var prevContent = File.ReadAllText(prevFile);
            var currentContent = File.ReadAllText(currentFile);

            var prevBlocks = DiffEngine.ParseStatements(prevContent);
            var currentBlocks = DiffEngine.ParseStatements(currentContent);

            // Build set of normalized content in current
            var currentSet = new HashSet<string>();
            foreach (var b in currentBlocks)
                currentSet.Add(b.Normalized);

            foreach (var block in prevBlocks)
            {
                if (!currentSet.Contains(block.Normalized))
                {
                    items.Add(new FixItem
                    {
                        Source = "Previous CREATE",
                        SourceFile = $"{prevVersion}/{schema}/{fileName}",
                        TargetFile = fileName,
                        Label = block.Label,
                        SqlContent = block.RawText.TrimEnd() + "\nGO\n",
                        SourceLine = block.StartLine
                    });
                }
            }
        }

        return items;
    }

    /// <summary>
    /// Finds UPGRADE statements not yet in the corresponding CREATE file.
    /// </summary>
    public List<FixItem> FindMissingFromUpgrade(string version, string schema)
    {
        var items = new List<FixItem>();
        var upgradeDir = Path.Combine(_rootFolder, "UPGRADE", version, schema);
        var createDir = Path.Combine(_rootFolder, "CREATE", version, schema);

        if (!Directory.Exists(upgradeDir) || !Directory.Exists(createDir))
            return items;

        var upgradeFiles = Directory.GetFiles(upgradeDir, "*.sql");

        // Load all CREATE content for matching
        var createFiles = Directory.GetFiles(createDir, "*.sql");
        var allCreateNormalized = new HashSet<string>();
        foreach (var cf in createFiles)
        {
            var blocks = DiffEngine.ParseStatements(File.ReadAllText(cf));
            foreach (var b in blocks)
                allCreateNormalized.Add(b.Normalized);
        }

        foreach (var upgradeFile in upgradeFiles)
        {
            var upgFileName = Path.GetFileName(upgradeFile);
            var targetFile = MapUpgradeToCreate(upgFileName, schema);

            if (targetFile == null) continue;

            // Check target exists
            var targetPath = Path.Combine(createDir, targetFile);
            if (!File.Exists(targetPath)) continue;

            var upgradeContent = File.ReadAllText(upgradeFile);
            var upgradeBlocks = DiffEngine.ParseStatements(upgradeContent);

            foreach (var block in upgradeBlocks)
            {
                if (!allCreateNormalized.Contains(block.Normalized))
                {
                    items.Add(new FixItem
                    {
                        Source = "UPGRADE",
                        SourceFile = $"{version}/{schema}/{upgFileName}",
                        TargetFile = targetFile,
                        Label = block.Label,
                        SqlContent = block.RawText.TrimEnd() + "\nGO\n",
                        SourceLine = block.StartLine
                    });
                }
            }
        }

        return items;
    }

    private static string? MapUpgradeToCreate(string upgradeFileName, string schema)
    {
        var upper = upgradeFileName.ToUpperInvariant();
        foreach (var (upgradeKey, createKey) in UpgradeToCreateMap)
        {
            if (upper.Contains(upgradeKey))
                return $"{schema}_{createKey}.sql";
        }

        // Direct name match for files like SCAH_UPDATE_FEATURE_TOGGLE_EXPIRY.sql
        // These exist in both UPGRADE and CREATE with the same name
        if (upper.Contains("UPDATE_FEATURE_TOGGLE") || upper.Contains("UPGRADE_METADATA"))
            return upgradeFileName.Replace("UPGRADE_", "CREATE_");

        return null;
    }

    /// <summary>
    /// Applies selected fix items by appending SQL to the target CREATE files.
    /// </summary>
    public int ApplyFixes(string version, string schema, List<FixItem> items)
    {
        var createDir = Path.Combine(_rootFolder, "CREATE", version, schema);
        int applied = 0;

        foreach (var item in items.Where(i => i.IsSelected))
        {
            var targetPath = Path.Combine(createDir, item.TargetFile);
            if (!File.Exists(targetPath)) continue;

            File.AppendAllText(targetPath, "\n" + item.SqlContent);
            applied++;
        }

        return applied;
    }
}
