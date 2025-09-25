using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private bool checkAccess;

        public string Server
        {
            get
            {
                return server;
            }
            set
            {
                server = value;
                OnPropertyChanged(nameof(Server));
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
                OnPropertyChanged(nameof(Username));
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
                OnPropertyChanged(nameof(Password));
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
                OnPropertyChanged(nameof(isWindowAuthentication));
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
                OnPropertyChanged(nameof(UtilityPath));

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
                OnPropertyChanged(nameof(CreatePath));
            }
        }

        public string UpgradePath
        {
            get
            {
                return upgradePath;
            }
            set
            {
                upgradePath = value;
                OnPropertyChanged(nameof(UpgradePath));
            }
        }
        public int NoOfProcess
        {
            get
            {
                return noOfProcess;
            }
            set
            {
                noOfProcess = value;
                //  OnPropertyChanged(nameof(NoOfProcess));
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
                OnPropertyChanged(nameof(IsFullAccess));


            }
        }

        [JsonIgnore]
        public bool CheckAccess
        {
            get
            {
                return checkAccess;
            }
            set
            {
                checkAccess = value;
                OnPropertyChanged(nameof(CheckAccess));
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
                //  OnPropertyChanged(nameof(ExcludedKeys));
            }
        }

        public ObservableCollection<HeaderEnvironment> Headers { get; set; }

        public List<Mappings> Mapping { get; set; }

        public ObservableCollection<Query> Queries { get; set; }

        public List<Language> Languages { get; set; }


        public ObservableCollection<Region> Regions { get; set; }

        public List<CustomerDataGrowth> CustomerDataGrowths { get; set; }

        public EmailSetting EmailSettings { get; set; }

        public List<SQLType> SQLTypes { get; set; }

        public SettingsObject()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void GetSettings(string path = "settings.set")
        {
            if (File.Exists(path))
            {
                //string settingsFile = File.ReadAllText(path);
                //SettingsObject settingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsFile);

                //this.Server = settingsObject.Server;
                //this.Password = settingsObject.Password;
                //this.Username = settingsObject.Username;
                //this.IsWindowsAuthentication = settingsObject.IsWindowsAuthentication;
                //this.UtilityPath = settingsObject.UtilityPath;
                //this.CreatePath = settingsObject.CreatePath;
                //this.UpgradePath = settingsObject.UpgradePath;
                //this.Headers = settingsObject.Headers;
                //this.Mapping = settingsObject.Mapping;
                //this.Queries = settingsObject.Queries == null ? new List<Query>() : settingsObject.Queries;
                //this.Languages = settingsObject.Languages == null ? new List<Language>() : settingsObject.Languages;
                //this.NoOfProcess = settingsObject.NoOfProcess;
                //this.Regions = settingsObject.Regions == null ? new List<Region>() : settingsObject.Regions;
                //this.CustomerDataGrowths = settingsObject.CustomerDataGrowths == null ? new List<CustomerDataGrowth>() : settingsObject.CustomerDataGrowths;
                //this.IsFullAccess = settingsObject.IsFullAccess;
                //this.excludedKeys = settingsObject.excludedKeys;
                //this.EmailSettings = settingsObject.EmailSettings == null ? new EmailSetting() : settingsObject.EmailSettings;
                //this.SQLTypes = settingsObject.SQLTypes == null ? new List<SQLType>() : settingsObject.SQLTypes;    

                //foreach (HeaderEnvironment headerEnvironment in this.Headers)
                //{
                //    if (!headerEnvironment.Headers.ContainsKey("X-Infor-TenantId"))
                //        headerEnvironment.Headers.Add("X-Infor-TenantId", headerEnvironment.TenantName);
                //    if (!headerEnvironment.Headers.ContainsKey("content-type"))
                //        headerEnvironment.Headers.Add("content-type", "application/json");
                //    if (!headerEnvironment.Headers.ContainsKey("x-fplm-schema"))
                //        headerEnvironment.Headers.Add("x-fplm-schema", "FSH1");
                //}
            }
        }

        public void GetSettings(bool isFile, string text = "")
        {
            if (!string.IsNullOrEmpty(text) && isFile)
            {
                //string settingsFile = text;//File.ReadAllText(path);
                //SettingsObject settingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsFile);

                //this.Server = settingsObject.Server;
                //this.Password = settingsObject.Password;
                //this.Username = settingsObject.Username;
                //this.IsWindowsAuthentication = settingsObject.IsWindowsAuthentication;
                //this.UtilityPath = settingsObject.UtilityPath;
                //this.CreatePath = settingsObject.CreatePath;
                //this.UpgradePath = settingsObject.UpgradePath;
                //this.Headers = settingsObject.Headers;
                //this.Mapping = settingsObject.Mapping;
                //this.Queries = settingsObject.Queries == null ? new List<Query>() : settingsObject.Queries;
                //this.Languages = settingsObject.Languages == null ? new List<Language>() : settingsObject.Languages;

                //this.Regions = settingsObject.Regions == null ? new List<Region>() : settingsObject.Regions;
                //this.CustomerDataGrowths = settingsObject.CustomerDataGrowths == null ? new List<CustomerDataGrowth>() : settingsObject.CustomerDataGrowths;
                //this.IsFullAccess = settingsObject.IsFullAccess;
                //this.excludedKeys = settingsObject.excludedKeys;
                //this.EmailSettings = settingsObject.EmailSettings == null ? new EmailSetting() : settingsObject.EmailSettings;
                //this.SQLTypes = settingsObject.SQLTypes == null ?  new List<SQLType>() : settingsObject.SQLTypes;   


                //foreach (HeaderEnvironment headerEnvironment in this.Headers)
                //{
                //    if (!headerEnvironment.Headers.ContainsKey("x-infor-tenant"))
                //        headerEnvironment.Headers.Add("x-infor-tenant", headerEnvironment.TenantName);
                //    if (!headerEnvironment.Headers.ContainsKey("content-type"))
                //        headerEnvironment.Headers.Add("content-type", "application/json");
                //    if (!headerEnvironment.Headers.ContainsKey("x-fplm-schema"))
                //        headerEnvironment.Headers.Add("x-fplm-schema", "FSH1");
                //}
            }
        }


        public void SaveSettings()
        {
            string jsonStr = JsonConvert.SerializeObject(this);            
            File.WriteAllText("settings.set", jsonStr);
        }

        public string RetStr()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
