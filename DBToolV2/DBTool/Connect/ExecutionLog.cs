using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBTool.Connect
{
    public class ExecutionLogEntry
    {
        public string TenantId { get; set; }
        public string SchemaName { get; set; }
        public string ChangesetId { get; set; }
        public string FileName { get; set; }
        public string Comment { get; set; }
        public string Query { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
        public string DateExecuted { get; set; }
        public string ExecutedBy { get; set; }
    }

    public class ExecutionLog
    {
        private static readonly string FullFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExecutionLog");
        private static readonly string FullFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExecutionLog", "ExecutionLog.json");

        private List<ExecutionLogEntry> _entries;

        public ExecutionLog()
        {
            _entries = Load();
        }

        /// <summary>
        /// Static helper to log any query execution from anywhere in the app.
        /// </summary>
        public static void LogQuery(string query, string tenantId, string status, string errorMessage = null)
        {
            try
            {
                var log = new ExecutionLog();
                log.Add(new ExecutionLogEntry
                {
                    TenantId = tenantId ?? "",
                    SchemaName = "",
                    ChangesetId = "",
                    FileName = "",
                    Comment = !string.IsNullOrEmpty(query) && query.Length > 200 ? query.Substring(0, 200) + "..." : query ?? "",
                    Query = query ?? "",
                    Version = "",
                    Status = status,
                    DateExecuted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ExecutedBy = Environment.UserName
                });
                log.Save();
            }
            catch { }
        }

        private List<ExecutionLogEntry> Load()
        {
            try
            {
                if (!Directory.Exists(FullFolder))
                    Directory.CreateDirectory(FullFolder);

                if (!File.Exists(FullFile))
                    return new List<ExecutionLogEntry>();

                string json = File.ReadAllText(FullFile);
                return JsonConvert.DeserializeObject<List<ExecutionLogEntry>>(json) ?? new List<ExecutionLogEntry>();
            }
            catch
            {
                return new List<ExecutionLogEntry>();
            }
        }

        public void Add(ExecutionLogEntry entry)
        {
            _entries.Add(entry);
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(FullFolder))
                    Directory.CreateDirectory(FullFolder);

                string json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
                File.WriteAllText(FullFile, json);
            }
            catch (Exception ex)
            {
                DBTool.Controls.ThemedDialog.Show($"Failed to save execution log: {ex.Message}", "Log Error");
            }
        }

        public List<ExecutionLogEntry> GetAll()
        {
            return _entries;
        }

        public List<ExecutionLogEntry> GetRecent(int count = 5)
        {
            return _entries.OrderByDescending(e => e.DateExecuted).Take(count).ToList();
        }

        public List<TopTenantExecution> GetTopTenants(int count = 10)
        {
            return _entries
                .Where(e => !string.IsNullOrEmpty(e.TenantId))
                .GroupBy(e => e.TenantId)
                .Select(g => new TopTenantExecution { TenantId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToList();
        }
    }

    public class TopTenantExecution
    {
        public string TenantId { get; set; }
        public int Count { get; set; }
    }
}
