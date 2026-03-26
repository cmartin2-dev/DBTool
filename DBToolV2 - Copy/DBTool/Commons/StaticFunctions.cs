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
using System.Threading.Tasks;

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

        public static List<Tenant> GetTenants()
        {
            if (Tenants != null)
                return Tenants;
            else
            {
                Tenants = new List<Tenant>();

                if (!File.Exists(StaticFunctions.SettingsFilename))
                {
                    throw new Exception("File not exists");
                }

                string settingsStr = File.ReadAllText(StaticFunctions.TenantFilename);
                Tenants = JsonConvert.DeserializeObject<List<Tenant>>(settingsStr);

                return Tenants;
            }

        }
    }
}
