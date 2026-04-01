using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DBTool.Commons
{
    public class SqlCompletionData : ICompletionData
    {
        public SqlCompletionData(string text, string description = null)
        {
            Text = text;
            Description = description;
        }

        public ImageSource Image => null;
        public string Text { get; }
        public object Content => Text;
        public object Description { get; }
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }

    public static class SqlCompletionProvider
    {
        private static readonly HashSet<string> SqlKeywords = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT",
            "ON", "AND", "OR", "NOT", "IN", "ORDER BY", "GROUP BY",
            "HAVING", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
            "DELETE", "CREATE", "ALTER", "DROP", "TABLE", "INDEX",
            "BEGIN", "END", "IF", "ELSE", "DECLARE", "TOP", "DISTINCT",
            "COUNT", "SUM", "AVG", "MIN", "MAX", "UNION", "ALL",
            "EXEC", "WITH", "NOLOCK", "AS", "BETWEEN", "LIKE",
            "EXISTS", "CASE", "WHEN", "THEN", "GO", "NULL", "IS"
        };

        public static List<ICompletionData> GetCompletions(string currentWord, string fullText, int caretOffset)
        {
            var results = new List<ICompletionData>();
            if (string.IsNullOrEmpty(currentWord) || currentWord.Length < 1)
                return results;

            string upper = currentWord.ToUpper();

            // Detect context: is the cursor right after FROM/JOIN/UPDATE/INTO?
            bool isTableContext = false;
            if (caretOffset > 0 && fullText.Length >= caretOffset)
            {
                string textBefore = fullText.Substring(0, caretOffset - currentWord.Length).TrimEnd();
                string lastWord = "";
                int i = textBefore.Length - 1;
                while (i >= 0 && char.IsLetter(textBefore[i]))
                {
                    lastWord = textBefore[i] + lastWord;
                    i--;
                }
                string lw = lastWord.ToUpper();
                isTableContext = lw == "FROM" || lw == "JOIN" || lw == "UPDATE" || lw == "INTO" || lw == "TABLE";
            }

            // SQL Keywords
            foreach (var kw in SqlKeywords)
            {
                if (kw.StartsWith(upper, StringComparison.OrdinalIgnoreCase))
                    results.Add(new SqlCompletionData(kw, "SQL Keyword"));
            }

            if (isTableContext)
            {
                // Only suggest table names
                foreach (var table in SchemaStore.TableColumns.Keys)
                {
                    if (table.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                        results.Add(new SqlCompletionData(table, "Table"));
                }
            }
            else
            {
                // Suggest columns from referenced tables
                var queryTables = SqlTableParser.ExtractTables(fullText);
                var queryColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in queryTables)
                {
                    var cols = SchemaStore.GetColumnsForTable(kvp.Value);
                    if (cols != null)
                        foreach (var c in cols) queryColumns.Add(c);
                }

                var columnsToShow = queryColumns.Count > 0 ? queryColumns :
                    SchemaStore.TableColumns.Values.SelectMany(c => c).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var col in columnsToShow)
                {
                    if (col.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                        results.Add(new SqlCompletionData(col, "Column"));
                }

                // Also suggest table names if no tables detected yet
                if (queryTables.Count == 0)
                {
                    foreach (var table in SchemaStore.TableColumns.Keys)
                    {
                        if (table.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                            results.Add(new SqlCompletionData(table, "Table"));
                    }
                }
            }

            return results.OrderBy(x => x.Text).ToList();
        }

        public static List<ICompletionData> GetAliasCompletions(string alias, string fullText)
        {
            var results = new List<ICompletionData>();
            var tables = SqlTableParser.ExtractTables(fullText);

            if (tables.TryGetValue(alias, out string actualTable))
            {
                var cols = SchemaStore.GetColumnsForTable(actualTable);
                if (cols != null)
                {
                    foreach (var col in cols.OrderBy(c => c))
                    {
                        results.Add(new SqlCompletionData(col, $"Column of {actualTable}"));
                    }
                }
            }

            return results;
        }
    }
}
