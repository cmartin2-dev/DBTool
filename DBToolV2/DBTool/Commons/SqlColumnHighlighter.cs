using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace DBTool.Commons
{
    public class TableSchema
    {
        public string table { get; set; }
        public string schema { get; set; }
        public List<string> columns { get; set; }
    }

    public class SchemaStore
    {
        private static Dictionary<string, HashSet<string>> _tableColumns;
        private static Dictionary<string, List<string>> _schemaToTables;
        private static readonly string SchemaFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "schema.json");

        public static Dictionary<string, HashSet<string>> TableColumns
        {
            get
            {
                if (_tableColumns == null) Load();
                return _tableColumns;
            }
        }

        public static Dictionary<string, List<string>> SchemaToTables
        {
            get
            {
                if (_schemaToTables == null) Load();
                return _schemaToTables;
            }
        }

        public static void Load()
        {
            _tableColumns = new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);
            _schemaToTables = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(SchemaFile))
                {
                    string json = File.ReadAllText(SchemaFile);
                    var schemas = JsonConvert.DeserializeObject<List<TableSchema>>(json);
                    if (schemas != null)
                    {
                        foreach (var s in schemas)
                        {
                            if (!string.IsNullOrEmpty(s.table) && s.columns != null)
                            {
                                _tableColumns[s.table] = new HashSet<string>(
                                    s.columns, StringComparer.OrdinalIgnoreCase);

                                // Build schema-to-tables mapping
                                if (!string.IsNullOrEmpty(s.schema))
                                {
                                    if (!_schemaToTables.ContainsKey(s.schema))
                                        _schemaToTables[s.schema] = new List<string>();
                                    _schemaToTables[s.schema].Add(s.table);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static HashSet<string> GetColumnsForTable(string tableName)
        {
            // Handle schema prefix like FSH603.STYLE -> STYLE
            string name = tableName;
            int dotIndex = tableName.LastIndexOf('.');
            if (dotIndex >= 0)
                name = tableName.Substring(dotIndex + 1);

            if (TableColumns.TryGetValue(name, out var cols))
                return cols;
            return null;
        }

        public static List<string> GetTablesForSchema(string schemaPrefix)
        {
            // Try exact match first (e.g. "SCAH")
            if (SchemaToTables.TryGetValue(schemaPrefix, out var tables))
                return tables;

            // Try stripping numeric suffix (e.g. "FSH1" -> "FSH", "FSH603" -> "FSH")
            var stripped = new string(schemaPrefix.TakeWhile(c => char.IsLetter(c)).ToArray());
            if (!string.IsNullOrEmpty(stripped) && SchemaToTables.TryGetValue(stripped, out var tables2))
                return tables2;

            return null;
        }
    }

    /// <summary>
    /// Simple SQL parser that extracts table names and aliases from FROM/JOIN clauses.
    /// </summary>
    public static class SqlTableParser
    {
        private static readonly Regex TablePattern = new Regex(
            @"(?:FROM|JOIN|UPDATE|DELETE\s+FROM|INTO)\s+(\[?\w+(?:\.\w+)?\]?)(?:\s+(?:AS\s+)?(\w+))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Returns a dictionary of alias/tableName -> actual table name (schema-stripped).
        /// e.g. "FROM SCAH.DATASCHEMAS DS" produces:
        ///   "SCAH.DATASCHEMAS" -> "DATASCHEMAS"
        ///   "DATASCHEMAS"      -> "DATASCHEMAS"
        ///   "DS"               -> "DATASCHEMAS"
        /// </summary>
        public static Dictionary<string, string> ExtractTables(string sql)
        {
            var tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(sql)) return tables;

            foreach (Match match in TablePattern.Matches(sql))
            {
                string rawName = match.Groups[1].Value.Trim('[', ']');
                string alias = match.Groups[2].Success ? match.Groups[2].Value : null;

                // Strip schema prefix: SCAH.DATASCHEMAS -> DATASCHEMAS
                string bareName = rawName;
                int dotIdx = rawName.LastIndexOf('.');
                if (dotIdx >= 0)
                    bareName = rawName.Substring(dotIdx + 1);

                // Store by full qualified name -> bare name
                if (!tables.ContainsKey(rawName))
                    tables[rawName] = bareName;

                // Store by bare table name
                if (!tables.ContainsKey(bareName))
                    tables[bareName] = bareName;

                // Store by alias
                if (!string.IsNullOrEmpty(alias) && !tables.ContainsKey(alias))
                    tables[alias] = bareName;
            }

            return tables;
        }

        /// <summary>
        /// Gets all valid column names from all tables in the SQL.
        /// </summary>
        public static HashSet<string> GetValidColumns(string sql)
        {
            var validColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tables = ExtractTables(sql);

            foreach (var kvp in tables)
            {
                var cols = SchemaStore.GetColumnsForTable(kvp.Value);
                if (cols != null)
                {
                    foreach (var col in cols)
                        validColumns.Add(col);
                }
            }

            return validColumns;
        }
    }

    /// <summary>
    /// AvalonEdit colorizer that highlights column names that belong to detected tables.
    /// Handles both bare column names and alias.column patterns (e.g. S.STYLEID).
    /// </summary>
    public class SqlColumnColorizer : DocumentColorizingTransformer
    {
        // Matches alias.column (e.g. S.STYLEID, SM.STYLEMEASID)
        private static readonly Regex AliasColumnPattern = new Regex(
            @"\b(\w+)\.(\w+)\b", RegexOptions.Compiled);

        // Matches standalone words
        private static readonly Regex WordPattern = new Regex(
            @"\b(\w+)\b", RegexOptions.Compiled);

        private static readonly HashSet<string> SqlKeywords = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER",
            "ON", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS",
            "NULL", "AS", "ORDER", "BY", "GROUP", "HAVING", "INSERT", "INTO",
            "VALUES", "UPDATE", "SET", "DELETE", "CREATE", "ALTER", "DROP",
            "TABLE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
            "BEGIN", "END", "IF", "ELSE", "THEN", "CASE", "WHEN", "DECLARE",
            "TOP", "DISTINCT", "COUNT", "SUM", "AVG", "MIN", "MAX", "UNION",
            "ALL", "GO", "EXEC", "EXECUTE", "WITH", "NOLOCK", "ASC", "DESC",
            "CROSS", "FULL", "APPLY", "OVER", "PARTITION", "ROW_NUMBER",
            "GETDATE", "ISNULL", "COALESCE", "CAST", "CONVERT", "NVARCHAR",
            "VARCHAR", "INT", "BIT", "DATETIME", "FLOAT", "DECIMAL"
        };

        private readonly SolidColorBrush _validColumnBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#2E7D32"));

        // Cache to avoid re-parsing on every line
        private string _cachedText;
        private Dictionary<string, string> _cachedTables;
        private HashSet<string> _cachedAllColumns;

        private void RefreshCache(string fullText)
        {
            if (fullText == _cachedText) return;

            _cachedText = fullText;
            _cachedTables = SqlTableParser.ExtractTables(fullText);
            _cachedAllColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in _cachedTables)
            {
                var cols = SchemaStore.GetColumnsForTable(kvp.Value);
                if (cols != null)
                    foreach (var c in cols) _cachedAllColumns.Add(c);
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            string fullText = CurrentContext.Document.Text;
            RefreshCache(fullText);

            if (_cachedTables == null || _cachedTables.Count == 0) return;
            if (_cachedAllColumns == null || _cachedAllColumns.Count == 0) return;

            int lineStart = line.Offset;
            string lineText = CurrentContext.Document.GetText(line);

            // First pass: handle alias.column patterns
            var handledRanges = new HashSet<int>();

            foreach (Match match in AliasColumnPattern.Matches(lineText))
            {
                string prefix = match.Groups[1].Value;
                string column = match.Groups[2].Value;

                if (_cachedTables.TryGetValue(prefix, out string actualTable))
                {
                    var tableCols = SchemaStore.GetColumnsForTable(actualTable);
                    if (tableCols != null && tableCols.Contains(column))
                    {
                        int colStart = lineStart + match.Groups[2].Index;
                        int colEnd = colStart + column.Length;

                        ChangeLinePart(colStart, colEnd, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(_validColumnBrush);
                        });
                    }

                    for (int i = match.Index; i < match.Index + match.Length; i++)
                        handledRanges.Add(i);
                }
            }

            // Second pass: bare column names
            foreach (Match match in WordPattern.Matches(lineText))
            {
                if (handledRanges.Contains(match.Index)) continue;

                string word = match.Value;
                if (SqlKeywords.Contains(word)) continue;
                if (_cachedTables.ContainsKey(word)) continue;

                if (_cachedAllColumns.Contains(word))
                {
                    int start = lineStart + match.Index;
                    int end = start + match.Length;

                    ChangeLinePart(start, end, element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(_validColumnBrush);
                    });
                }
            }
        }
    }
}
