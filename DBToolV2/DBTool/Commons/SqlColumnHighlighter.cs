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
        public List<string> columns { get; set; }
    }

    public class SchemaStore
    {
        private static Dictionary<string, HashSet<string>> _tableColumns;
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

        public static void Load()
        {
            _tableColumns = new Dictionary<string, HashSet<string>>(
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
    }

    /// <summary>
    /// Simple SQL parser that extracts table names and aliases from FROM/JOIN clauses.
    /// </summary>
    public static class SqlTableParser
    {
        private static readonly Regex TablePattern = new Regex(
            @"(?:FROM|JOIN)\s+(\[?[\w.]+\]?)(?:\s+(?:AS\s+)?(\w+))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Returns a dictionary of alias/tableName -> actual table name
        /// </summary>
        public static Dictionary<string, string> ExtractTables(string sql)
        {
            var tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(sql)) return tables;

            foreach (Match match in TablePattern.Matches(sql))
            {
                string tableName = match.Groups[1].Value.Trim('[', ']');
                string alias = match.Groups[2].Success ? match.Groups[2].Value : null;

                // Store by actual table name
                if (!tables.ContainsKey(tableName))
                    tables[tableName] = tableName;

                // Store by alias
                if (!string.IsNullOrEmpty(alias) && !tables.ContainsKey(alias))
                    tables[alias] = tableName;
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
    /// </summary>
    public class SqlColumnColorizer : DocumentColorizingTransformer
    {
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
            (Color)ColorConverter.ConvertFromString("#2E7D32")); // green

        protected override void ColorizeLine(DocumentLine line)
        {
            string fullText = CurrentContext.Document.Text;
            var validColumns = SqlTableParser.GetValidColumns(fullText);

            if (validColumns.Count == 0) return;

            int lineStart = line.Offset;
            string lineText = CurrentContext.Document.GetText(line);

            foreach (Match match in WordPattern.Matches(lineText))
            {
                string word = match.Value;

                // Skip SQL keywords and table names
                if (SqlKeywords.Contains(word)) continue;

                // Check if this word is a valid column
                if (validColumns.Contains(word))
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
