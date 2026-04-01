using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBTool.Commons
{
    public class QueryHistoryEntry
    {
        public string Query { get; set; }
        public string TenantId { get; set; }
        public string DateExecuted { get; set; }
    }

    public static class QueryHistory
    {
        private static readonly string HistoryFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "QueryHistory", "history.json");
        private static readonly int MaxEntries = 50;

        public static void Add(string query, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                var entries = Load();

                // Don't add duplicates of the same query for the same tenant
                entries.RemoveAll(e => e.Query == query && e.TenantId == tenantId);

                entries.Insert(0, new QueryHistoryEntry
                {
                    Query = query,
                    TenantId = tenantId ?? "",
                    DateExecuted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                // Keep only the last N entries
                if (entries.Count > MaxEntries)
                    entries = entries.Take(MaxEntries).ToList();

                Save(entries);
            }
            catch { }
        }

        public static List<QueryHistoryEntry> Load()
        {
            try
            {
                string dir = Path.GetDirectoryName(HistoryFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(HistoryFile))
                    return new List<QueryHistoryEntry>();

                string json = File.ReadAllText(HistoryFile);
                return JsonConvert.DeserializeObject<List<QueryHistoryEntry>>(json)
                    ?? new List<QueryHistoryEntry>();
            }
            catch
            {
                return new List<QueryHistoryEntry>();
            }
        }

        private static void Save(List<QueryHistoryEntry> entries)
        {
            try
            {
                string dir = Path.GetDirectoryName(HistoryFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
                File.WriteAllText(HistoryFile, json);
            }
            catch { }
        }

        public static List<QueryHistoryEntry> GetForTenant(string tenantId)
        {
            return Load().Where(e => 
                string.IsNullOrEmpty(tenantId) || 
                e.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
