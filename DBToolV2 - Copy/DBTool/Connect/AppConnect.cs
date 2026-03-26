using DBTool;
using DBTool.Commons;
using Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DBTool.Connect
{
    public class AppConnect
    {
        public SettingsObject settingsObject;
        public AppConnect()
        {

            try
            {
                GetSettingsObject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GetSettingsObject()
        {
            if (!File.Exists(StaticFunctions.SettingsFilename))
            {
                throw new Exception("File not exists");
            }

            string settingsStr = File.ReadAllText(StaticFunctions.SettingsFilename);
            settingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsStr);
            if (settingsObject.EnvironmentServers == null)
                settingsObject.EnvironmentServers = new ObservableCollection<EnvironmentServer>();
           // StaticFunctions.OriginalSettingsObject = JsonConvert.DeserializeObject<SettingsObject>(settingsStr);
            StaticFunctions.OrigingalSettingsObjectStr = settingsStr;
        }

        public void SaveSettings()
        {
            try
            {
                this.settingsObject.SaveSettings();
                this.settingsObject.CheckAccess = this.settingsObject.IsFullAccess;
                StaticFunctions.OrigingalSettingsObjectStr = this.settingsObject.RetStr();
                //StaticFunctions.OriginalSettingsObject = this.settingsObject;



                MessageBox.Show("Settings saved successfully", "Save Settings", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveSettings2()
        {
            try
            {
                this.settingsObject.SaveSettings();
                this.settingsObject.CheckAccess = this.settingsObject.IsFullAccess;
                StaticFunctions.OrigingalSettingsObjectStr = this.settingsObject.RetStr();
                // StaticFunctions.OriginalSettingsObject = this.settingsObject;

            }
            catch (Exception ex)
            {
            }
        }

        public void RevertSettings()
        {
            GetSettingsObject();
        }

        public bool CheckDirty()
        {
            string original = Utilities.BeautifyJson(StaticFunctions.OrigingalSettingsObjectStr); //JsonConvert.SerializeObject(StaticFunctions.OriginalSettingsObject);
           string updated= Utilities.BeautifyJson(JsonConvert.SerializeObject(this.settingsObject));
            if (original != updated)
            {
                return false;
            }
            return true;
        }
    }
}
