using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Settings
    {
        public string ConnectionString { get; set; }
        public string TMConnectionString { get; set; }
        public string ScriptsPath { get; set; }
        public string Tenant { get; set; }
        public string AppVersion { get; set; }
        public string NewDbVersion { get; set; }
        public string UtilityPath { get; set; }
        public string LayoutId { get; set; }
        public string ScriptsRoot { get; set; }
        public bool IsJenkins { get; set; }
        public string ScriptLayoutAs { get; set; }
        public string FbkFile { get; set; }
        public string LocaleCSV { get; set; }
        public string DevelopmentVersion { get; set; }
        public string S3BucketName { get; set; }
        public string LiquibaseRun { get; set; }

        public string LiquibaseDriver { get; set; }
    }
}
