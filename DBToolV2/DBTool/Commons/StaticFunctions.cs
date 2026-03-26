using DBTool.Connect;
using Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBTool
{
    public class StaticFunctions
    {
        private static bool _isFullAccess = false;

        public static string fullAdminPassword = "adminFullAccess123";
        public static string SettingsFilename = "settings.set";
        public static string TenantFilename = "tenantList.json";

        public static AppConnect AppConnection { get; set; }
        public static SettingsObject OriginalSettingsObject { get; set; }

        public static string OrigingalSettingsObjectStr { get; set; }

        public static string CurrentUser { get { return Environment.UserName; } }

        public static List<Tenant> Tenants { get; set; }

        public static HeaderEnvironment GetHeaderEnvironment(int? id)
        {
            if (AppConnection.settingsObject != null && AppConnection.settingsObject.Headers != null)
                return AppConnection.settingsObject.Headers.FirstOrDefault(x => x.Id == id);
            return null;
        }

        public static HeaderEnvironment GetHeaderEnvironment(string tenantName)
        {
            if (AppConnection.settingsObject != null && AppConnection.settingsObject.Headers != null)
                return AppConnection.settingsObject.Headers.FirstOrDefault(x => x.TenantName == tenantName);
            return null;
        }

        public static bool IsFullAccess
        {
            get
            {
                return _isFullAccess;
            }
            set
            {
                _isFullAccess= value;
                OnStaticPropertyChanged(nameof(IsFullAccess));
            }
        }

        public static bool forTestingEnvironmentsFullAccess { get { return true; } }

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        private static void OnStaticPropertyChanged(string propertyName)
            => StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Parses a Liquibase SQL-formatted changeset file into a Changeset object.
        /// Extracts author, id, params, comment, and the SQL script body.
        /// Expected format:
        /// --changeset author:id  splitStatements:true endDelimiter:GO
        /// --param paramName:paramType
        /// --comment: description
        /// SQL SCRIPT BODY HERE
        /// </summary>
        public static Changeset ParseChangeset(string input)
        {
            var changeset = new Changeset();
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var scriptLines = new List<string>();
            bool headerDone = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (!headerDone && trimmed.StartsWith("--changeset "))
                {
                    var content = trimmed.Substring("--changeset ".Length).Trim();
                    var tokens = Regex.Split(content, @"\s{2,}|\t+");
                    if (tokens.Length > 0)
                    {
                        var authorId = tokens[0];
                        var colonIndex = authorId.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            changeset.Author = authorId.Substring(0, colonIndex);
                            changeset.Id = authorId.Substring(colonIndex + 1);
                        }
                    }
                }
                else if (!headerDone && trimmed.StartsWith("--param "))
                {
                    changeset.Params.Add(trimmed.Substring("--param ".Length).Trim());
                }
                else if (!headerDone && trimmed.StartsWith("--comment:"))
                {
                    changeset.Comment = trimmed.Substring("--comment:".Length).Trim();
                    headerDone = true;
                }
                else if (headerDone)
                {
                    scriptLines.Add(line);
                }
            }

            changeset.Script = string.Join(Environment.NewLine, scriptLines).Trim();
            return changeset;
        }

        /// <summary>
        /// Parses a Liquibase SQL file that may contain multiple changesets.
        /// Each --changeset line starts a new changeset block.
        /// </summary>
        public static List<Changeset> ParseChangesets(string fileContent)
        {
            var changesets = new List<Changeset>();
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var currentBlock = new List<string>();

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("--changeset ") && currentBlock.Count > 0)
                {
                    changesets.Add(ParseChangeset(string.Join(Environment.NewLine, currentBlock)));
                    currentBlock.Clear();
                }
                currentBlock.Add(line);
            }

            if (currentBlock.Count > 0)
                changesets.Add(ParseChangeset(string.Join(Environment.NewLine, currentBlock)));

            return changesets;
        }

        /// <summary>
        /// Builds a SQL script that:
        /// 1. Checks if the changeset already exists in DATABASECHANGELOG
        /// 2. If not, executes the changeset script and inserts the changelog entry
        /// </summary>
        public static string BuildChangesetExecutionScript(Changeset changeset, string filename)
        {
            var escapedId = changeset.Id?.Replace("'", "''") ?? "";
            var escapedAuthor = changeset.Author?.Replace("'", "''") ?? "";
            var escapedComment = changeset.Comment?.Replace("'", "''") ?? "";
            var escapedFilename = filename?.Replace("'", "''") ?? "";

            var sb = new StringBuilder();
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM DATABASECHANGELOG WHERE ID = '{escapedId}' AND AUTHOR = '{escapedAuthor}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine();
            sb.AppendLine(changeset.Script);
            sb.AppendLine();
            sb.AppendLine($"    INSERT INTO DATABASECHANGELOG (ID, AUTHOR, FILENAME, DATEEXECUTED, ORDEREXECUTED, EXECTYPE, DESCRIPTION, COMMENTS)");
            sb.AppendLine($"    VALUES ('{escapedId}', '{escapedAuthor}', '{escapedFilename}', GETDATE(), (SELECT ISNULL(MAX(ORDEREXECUTED), 0) + 1 FROM DATABASECHANGELOG), 'EXECUTED', 'sql', '{escapedComment}')");
            sb.AppendLine();
            sb.AppendLine("END");

            return sb.ToString();
        }

        /// <summary>
        /// Reads all version folder names from the upgrade folder path.
        /// Returns them sorted by version (ascending).
        /// </summary>
        public static List<string> GetVersionFolders(string upgradeFolderPath)
        {
            if (!Directory.Exists(upgradeFolderPath))
                return new List<string>();

            return Directory.GetDirectories(upgradeFolderPath)
                .Select(d => System.IO.Path.GetFileName(d))
                .OrderBy(v => v)
                .ToList();
        }

        /// <summary>
        /// Parses a ChangeSet_master XML file and returns the ordered list of include file paths.
        /// </summary>
        public static List<string> ParseChangeSetMasterXml(string xmlFilePath)
        {
            var files = new List<string>();
            if (!File.Exists(xmlFilePath))
                return files;

            var doc = XDocument.Load(xmlFilePath);
            XNamespace ns = "http://www.liquibase.org/xml/ns/dbchangelog";

            foreach (var include in doc.Descendants(ns + "include"))
            {
                var file = include.Attribute("file")?.Value;
                if (!string.IsNullOrEmpty(file))
                    files.Add(file);
            }

            return files;
        }

        /// <summary>
        /// Reads and parses all changesets for selected versions from the upgrade folder.
        /// Returns a list of VersionChangeset objects with changesets in execution order.
        /// Parameters in the SQL scripts (e.g. ${schemaName}) are replaced with the provided values.
        /// </summary>
        public static List<VersionChangeset> ReadUpgradeFolder(string upgradeFolderPath, List<string> selectedVersions, Dictionary<string, string> parameters = null, List<string> modules = null)
        {
            var result = new List<VersionChangeset>();

            // Default: SCAH first, then FSH
            if (modules == null || modules.Count == 0)
                modules = new List<string> { "SCAH", "FSH" };

            foreach (var version in selectedVersions.OrderBy(v => v))
            {
                var versionPath = System.IO.Path.Combine(upgradeFolderPath, version);
                if (!Directory.Exists(versionPath))
                    continue;

                var versionChangeset = new VersionChangeset
                {
                    Version = version
                };

                foreach (var schema in modules)
                {
                    var masterXmlPath = System.IO.Path.Combine(versionPath, $"ChangeSet_master_{schema}.xml");
                    var orderedFiles = ParseChangeSetMasterXml(masterXmlPath);

                    foreach (var relativeFile in orderedFiles)
                    {
                        var sqlFilePath = System.IO.Path.Combine(versionPath, relativeFile);
                        if (!File.Exists(sqlFilePath))
                            continue;

                        versionChangeset.FileDirectories.Add(sqlFilePath);

                        var fileContent = File.ReadAllText(sqlFilePath);

                        // Replace parameters (e.g. ${schemaName} -> actual value)
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                fileContent = fileContent.Replace($"${{{param.Key}}}", param.Value);
                            }
                        }

                        var changesets = ParseChangesets(fileContent);
                        foreach (var cs in changesets)
                        {
                            versionChangeset.ChangeSetItem.Add(new ChangesetItem
                            {
                                Id = cs.Id,
                                Author = cs.Author,
                                Comment = cs.Comment,
                                Script = cs.Script,
                                FileName = relativeFile,
                                Schema = schema
                            });
                        }
                    }
                }

                result.Add(versionChangeset);
            }

            return result;
        }

        /// <summary>
        /// Builds the full execution SQL for all changesets across selected versions.
        /// Each changeset is wrapped with an IF NOT EXISTS check against DATABASECHANGELOG.
        /// </summary>
        public static string BuildUpgradeScript(List<VersionChangeset> versionChangesets)
        {
            var sb = new StringBuilder();

            foreach (var vc in versionChangesets)
            {
                sb.AppendLine($"-- =============================================");
                sb.AppendLine($"-- Version: {vc.Version}");
                sb.AppendLine($"-- =============================================");
                sb.AppendLine();

                foreach (var item in vc.ChangeSetItem)
                {
                    var changeset = new Changeset
                    {
                        Id = item.Id,
                        Author = item.Author,
                        Comment = item.Comment,
                        Script = item.Script
                    };

                    sb.AppendLine(BuildChangesetExecutionScript(changeset, item.FileName));
                    sb.AppendLine("GO");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static List<Tenant> GetTenants()
        {
            if (Tenants != null)
                return Tenants;
            else
            {
                Tenants = new List<Tenant>();

                if (!File.Exists(StaticFunctions.TenantFilename))
                {
                    File.WriteAllText(StaticFunctions.TenantFilename, "[]");
                    return Tenants;
                }

                string settingsStr = File.ReadAllText(StaticFunctions.TenantFilename);
                Tenants = JsonConvert.DeserializeObject<List<Tenant>>(settingsStr) ?? new List<Tenant>();

                return Tenants;
            }

        }
    }
}
