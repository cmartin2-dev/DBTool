using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace DBTool.Connect
{
    public class VersionInfo
    {
        public string version { get; set; }
        public string downloadUrl { get; set; }
        public string changelog { get; set; }
        public string changelogdDir { get; set; }
    }

    public class AppUpdater
    {
        private static readonly string VersionUrl = "https://raw.githubusercontent.com/cmartin2-dev/DBTool/main/version.json";
        private static readonly HttpClient _client = new HttpClient();

        static AppUpdater()
        {
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("DBTool-Updater");
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        public static string GetCurrentVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "0.0.0.0";
        }

        public static async Task<VersionInfo> CheckForUpdate()
        {
            try
            {
                string json = await _client.GetStringAsync(VersionUrl);
                var info = JsonConvert.DeserializeObject<VersionInfo>(json);
                return info;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsNewerVersion(string remoteVersion, string currentVersion)
        {
            try
            {
                var remote = new Version(remoteVersion);
                var current = new Version(currentVersion);
                return remote > current;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> DownloadUpdate(string downloadUrl, Action<int> progressCallback = null)
        {
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "DBToolUpdate");
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                Directory.CreateDirectory(tempFolder);

                string zipPath = Path.Combine(tempFolder, "DBTool.zip");

                using (var response = await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipPath, FileMode.Create))
                    {
                        var buffer = new byte[8192];
                        long downloaded = 0;
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloaded += bytesRead;

                            if (totalBytes > 0)
                                progressCallback?.Invoke((int)(downloaded * 100 / totalBytes));
                        }
                    }
                }

                // Extract
                string extractPath = Path.Combine(tempFolder, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                return extractPath;
            }
            catch
            {
                return null;
            }
        }

        public static void ApplyUpdate(string extractedPath)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string batchFile = Path.Combine(Path.GetTempPath(), "DBToolUpdate.bat");

                // Files to preserve during update
                string[] preserveFiles = { "settings.set", "tenantList.json", "theme.cfg" };
                string backupDir = Path.Combine(Path.GetTempPath(), "DBToolBackup");

                string backupCommands = "";
                string restoreCommands = "";

                foreach (var file in preserveFiles)
                {
                    backupCommands += $"if exist \"{Path.Combine(appDir, file)}\" copy /y \"{Path.Combine(appDir, file)}\" \"{Path.Combine(backupDir, file)}\" >nul\r\n";
                    restoreCommands += $"if exist \"{Path.Combine(backupDir, file)}\" copy /y \"{Path.Combine(backupDir, file)}\" \"{Path.Combine(appDir, file)}\" >nul\r\n";
                }

                string script = $@"
@echo off
echo Waiting for DBTool to close...
timeout /t 3 /nobreak >nul
echo Backing up user files...
if not exist ""{backupDir}"" mkdir ""{backupDir}""
{backupCommands}
echo Copying update files...
xcopy ""{extractedPath}\*"" ""{appDir}"" /s /y /q
echo Restoring user files...
{restoreCommands}
echo Starting DBTool...
start """" ""{Path.Combine(appDir, "DBTool.exe")}""
rmdir /s /q ""{backupDir}"" >nul 2>&1
del ""%~f0""
";
                File.WriteAllText(batchFile, script);

                Process.Start(new ProcessStartInfo
                {
                    FileName = batchFile,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                System.Windows.Application.Current.Shutdown();
            }
            catch { }
        }
    }
}
