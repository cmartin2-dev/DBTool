using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class FeatureToggle : INotifyPropertyChanged
    {

        private string _appFeatureId;
        private string _key;
        private string _displayName;
        private string _enabled;
        private string _visible;
        private string _description;
        private string _version;
        private string _expiryDate;
       // private string _metaData;

        public string AppFeatureId
        {
            get
            {
                return _appFeatureId;
            }
            set
            {
                _appFeatureId = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("AppFeatureId"));

            }
        }
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Key"));

            }
        }
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("DisplayName"));

            }
        }
        public string Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Enabled"));

            }
        }
        public string Visible
        {
            get
            {

                return _visible;
            }
            set
            {
                _visible = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Visible"));

            }
        }
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Description"));

            }
        }
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Version"));

            }
        }
        public string ExpiryDate
        {
            get
            {
                string date = string.Empty;
                if (!string.IsNullOrEmpty(_expiryDate))
                {
                   date  = String.Format("{0:MM/dd/yyyy}", DateTime.Parse(_expiryDate));

                }
                return String.Format("{0:MM/dd/yyyy}", date);
            }
            set
            {
                _expiryDate = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("ExpiryDate"));

            }
        }

        public string MetaData
        {
            get;set;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }
}
