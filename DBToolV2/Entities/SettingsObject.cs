using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class SettingsObject : INotifyPropertyChanged
    {

        private string server;
        private string username;
        private string password;
        private bool isWindowAuthentication;
        private string utilityPath;
        private string createPath;
        private string upgradePath;
        private int noOfProcess;
        private bool isFullAccess;
        private string excludedKeys;


        public string Server
        {
            get
            {
                return server;
            }
            set
            {
                server = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Server"));
            }
        }
        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Username"));
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Password"));
            }
        }
        public bool IsWindowsAuthentication
        {
            get
            {
                return isWindowAuthentication;
            }
            set
            {
                isWindowAuthentication = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("IsWindowsAuthentication"));
            }
        }

        public string UtilityPath
        {
            get
            {
                return utilityPath;
            }
            set
            {
                utilityPath = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("UtilityPath"));

            }
        }

        public string CreatePath
        {
            get
            {
                return createPath;
            }
            set
            {
                createPath = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("CreatePath"));
            }
        }

        public string UpdgradePath
        {
            get
            {
                return upgradePath;
            }
            set
            {
                upgradePath = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("UpgradePath"));
            }
        }
        public int NoOfProcess {
            get
            {
                return noOfProcess;
            }
            set
            {
                noOfProcess = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("NoOfProcess"));
            }
        }
        public bool IsFullAccess
        {
            get
            {
                return isFullAccess;
            }
            set
            {
                isFullAccess = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("IsFullAccess"));
            }
        }

        public string ExcludedKeys
        {
            get
            {
                return excludedKeys;
            }
            set
            {
                excludedKeys = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("ExcludedKeys"));
            }
        }

        public List<HeaderEnvironment> Headers { get; set; }

        public List<Mappings> Mapping { get; set; }

        public List<Query> Queries { get; set; }

        public List<Language> Languages { get; set; }


        public List<Region> Regions { get; set; }

        public List<CustomerDataGrowth> CustomerDataGrowths { get; set; }

        public EmailSetting EmailSettings { get; set; }

        public List<SQLType> SQLTypes { get; set; }

        public SettingsObject()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        public void GetSettings(string path = "settings.set")
        {
            if (File.Exists(path))
            {
                string settingsFile = File.ReadAllText(path);
                SettingsObject settingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsFile);

                this.Server = settingsObject.Server;
                this.Password = settingsObject.Password;
                this.Username = settingsObject.Username;
                this.IsWindowsAuthentication = settingsObject.IsWindowsAuthentication;
                this.UtilityPath = settingsObject.UtilityPath;
                this.CreatePath = settingsObject.CreatePath;
                this.UpdgradePath = settingsObject.UpdgradePath;
                this.Headers = settingsObject.Headers;
                this.Mapping = settingsObject.Mapping;
                this.Queries = settingsObject.Queries == null ? new List<Query>() : settingsObject.Queries;
                this.Languages = settingsObject.Languages == null ? new List<Language>() : settingsObject.Languages;
                this.NoOfProcess = settingsObject.NoOfProcess;
                this.Regions = settingsObject.Regions == null ? new List<Region>() : settingsObject.Regions;
                this.CustomerDataGrowths = settingsObject.CustomerDataGrowths == null ? new List<CustomerDataGrowth>() : settingsObject.CustomerDataGrowths;
                this.IsFullAccess = settingsObject.IsFullAccess;
                this.excludedKeys = settingsObject.excludedKeys;
                this.EmailSettings = settingsObject.EmailSettings == null ? new EmailSetting() : settingsObject.EmailSettings;
                this.SQLTypes = settingsObject.SQLTypes == null ? new List<SQLType>() : settingsObject.SQLTypes;    

                foreach (HeaderEnvironment headerEnvironment in this.Headers)
                {
                    if (!headerEnvironment.Headers.ContainsKey("X-Infor-TenantId"))
                        headerEnvironment.Headers.Add("X-Infor-TenantId", headerEnvironment.TenantName);
                    if (!headerEnvironment.Headers.ContainsKey("content-type"))
                        headerEnvironment.Headers.Add("content-type", "application/json");
                    if (!headerEnvironment.Headers.ContainsKey("x-fplm-schema"))
                        headerEnvironment.Headers.Add("x-fplm-schema", "FSH1");
                }
            }
        }

        public void GetSettings(bool isFile, string text = "")
        {
            if (!string.IsNullOrEmpty(text) && isFile)
            {
                string settingsFile = text;//File.ReadAllText(path);
                SettingsObject settingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsFile);

                this.Server = settingsObject.Server;
                this.Password = settingsObject.Password;
                this.Username = settingsObject.Username;
                this.IsWindowsAuthentication = settingsObject.IsWindowsAuthentication;
                this.UtilityPath = settingsObject.UtilityPath;
                this.CreatePath = settingsObject.CreatePath;
                this.UpdgradePath = settingsObject.UpdgradePath;
                this.Headers = settingsObject.Headers;
                this.Mapping = settingsObject.Mapping;
                this.Queries = settingsObject.Queries == null ? new List<Query>() : settingsObject.Queries;
                this.Languages = settingsObject.Languages == null ? new List<Language>() : settingsObject.Languages;

                this.Regions = settingsObject.Regions == null ? new List<Region>() : settingsObject.Regions;
                this.CustomerDataGrowths = settingsObject.CustomerDataGrowths == null ? new List<CustomerDataGrowth>() : settingsObject.CustomerDataGrowths;
                this.IsFullAccess = settingsObject.IsFullAccess;
                this.excludedKeys = settingsObject.excludedKeys;
                this.EmailSettings = settingsObject.EmailSettings == null ? new EmailSetting() : settingsObject.EmailSettings;
                this.SQLTypes = settingsObject.SQLTypes == null ?  new List<SQLType>() : settingsObject.SQLTypes;   


                foreach (HeaderEnvironment headerEnvironment in this.Headers)
                {
                    if (!headerEnvironment.Headers.ContainsKey("x-infor-tenant"))
                        headerEnvironment.Headers.Add("x-infor-tenant", headerEnvironment.TenantName);
                    if (!headerEnvironment.Headers.ContainsKey("content-type"))
                        headerEnvironment.Headers.Add("content-type", "application/json");
                    if (!headerEnvironment.Headers.ContainsKey("x-fplm-schema"))
                        headerEnvironment.Headers.Add("x-fplm-schema", "FSH1");
                }
            }
        }


        public void SaveSettings()
        {
            string jsonStr = JsonConvert.SerializeObject(this);
            File.WriteAllText("settings.set", jsonStr);
        }
    }
}
