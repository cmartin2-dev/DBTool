using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DBTool.Commons
{
    public static class SqlFormatter
    {
        private static readonly string[] MajorKeywords = {
            "SELECT", "FROM", "WHERE", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
            "FULL JOIN", "CROSS JOIN", "JOIN", "ON", "AND", "OR",
            "ORDER BY", "GROUP BY", "HAVING", "INSERT INTO", "VALUES",
            "UPDATE", "SET", "DELETE FROM", "CREATE TABLE", "ALTER TABLE",
            "DROP TABLE", "UNION ALL", "UNION", "BEGIN", "END",
            "IF", "ELSE", "DECLARE", "EXEC", "EXECUTE"
        };

        public static string Format(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            // Normalize whitespace
            string result = Regex.Replace(sql.Trim(), @"\s+", " ");

            // Add newlines before major keywords
            foreach (var keyword in MajorKeywords)
            {
                result = Regex.Replace(result,
                    $@"(?<!\w){Regex.Escape(keyword)}(?!\w)",
                    $"\n{keyword}",
                    RegexOptions.IgnoreCase);
            }

            // Indent after SELECT (columns)
            result = Regex.Replace(result, @",\s*", ",\n    ");

            // Clean up
            var sb = new StringBuilder();
            var lines = result.Split('\n');
            int indent = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string upper = line.ToUpper();

                // Decrease indent for END
                if (upper.StartsWith("END")) indent = Math.Max(0, indent - 1);

                // Add indent
                sb.AppendLine(new string(' ', indent * 4) + line);

                // Increase indent after BEGIN
                if (upper.StartsWith("BEGIN")) indent++;
            }

            return sb.ToString().Trim();
        }
    }
}
