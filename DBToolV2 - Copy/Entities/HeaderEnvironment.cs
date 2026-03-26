using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class HeaderEnvironment : INotifyPropertyChanged, ICloneable
    {

        private string _envName = string.Empty;
        private int _id;
        private string _tenantName;
        private Dictionary<string, string> _headers;
        private string _endPoint;
        private string _clientId;
        private string _clientSecret;
        private string _resource;
        private string _tokenURL;
        private string _username;
        private string _password;
        private string _token;
        private bool _isOAuth1;
        private SQLDBInfo _SQLDBInfo;
        private string _defaultFSH;
        private string _IDMTask;
        private string _ThumbnailTask;

        private string _IONAPI;

        private string _TenantList;

        private bool _GetDataGrowth;

        private bool _isUS;
        private bool _isEU;

        public int Id
        {
            get { return _id; }

            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string EnvironmentName
        {
            get
            {

                if (string.IsNullOrEmpty(_envName))
                {
                    _envName = TenantName;
                }
                return _envName;
            }
            set
            {
                _envName = value;
                OnPropertyChanged(nameof(EnvironmentName));
            }
        }
        public string TenantName
        {
            get
            {
                return _tenantName;
            }

            set
            {
                _tenantName = value;

                OnPropertyChanged(nameof(TenantName));
            }
        }
        public Dictionary<string, string> Headers
        {
            get { return _headers; }
            set
            {
                _headers = value;

                OnPropertyChanged(nameof(Headers));
            }
        }


        public string EndPoint
        {
            get
            {
                return _endPoint;
            }
            set
            {
                _endPoint = value;
                OnPropertyChanged(nameof(EndPoint));
            }
        }

        public string ClientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                _clientId = value;
                OnPropertyChanged(nameof(ClientId));
            }
        }
        public string ClientSecret
        {
            get
            {
                return _clientSecret;
            }
            set
            {
                _clientSecret = value;
                OnPropertyChanged(nameof(ClientSecret));
            }
        }
        public string Resource
        {
            get
            {
                return _resource;
            }
            set
            {
                _resource = value;
                OnPropertyChanged(nameof(Resource));
            }
        }

        public string TokenUrl
        {
            get
            {
                return _tokenURL;
            }
            set
            {
                _tokenURL = value;
                OnPropertyChanged(nameof(TokenUrl));
            }
        }

        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
                OnPropertyChanged(nameof(Token));
            }
        }


        public bool isOAuth1
        {
            get
            {
                return _isOAuth1;
            }
            set
            {
                _isOAuth1 = value;
                OnPropertyChanged(nameof(isOAuth1));
            }
        }

        public SQLDBInfo SQLDBInfo
        {
            get
            { return _SQLDBInfo; }
            set
            {
                _SQLDBInfo = value;
                OnPropertyChanged(nameof(SQLDBInfo));
            }
        }

        public string DefaultFSH
        {
            get
            { return _defaultFSH; }
            set
            {
                _defaultFSH = value;
                OnPropertyChanged(nameof(DefaultFSH));
            }
        }

        public string IDMTask
        {
            get
            { return _IDMTask; }
            set
            {
                _IDMTask = value;
                OnPropertyChanged(nameof(IDMTask));
            }
        }
        public string ThumbnailTask
        {
            get
            {
                return _ThumbnailTask;
            }
            set
            {
                _ThumbnailTask = value;
                OnPropertyChanged(nameof(ThumbnailTask));
            }
        }

        public string IONAPI
        {

            get
            {
                return _IONAPI;
            }
            set
            {
                _IONAPI = value;
                OnPropertyChanged(nameof(IONAPI));
            }
        }

        public string TenantList
        {
            get
            { return _TenantList; }
            set
            {
                _TenantList = value;
                OnPropertyChanged(nameof(TenantList));
            }
        }

        public bool GetDataGrowth
        {
            get
            {
                return _GetDataGrowth;
            }
            set
            {
                _GetDataGrowth = value;
                OnPropertyChanged(nameof(GetDataGrowth));
            }
        }

        public bool isUS
        {

            get
            {
                return _isUS;
            }
            set
            {
                _isUS = value;
                OnPropertyChanged(nameof(isUS));
            }
        }
        public bool isEU
        {
            get
            {
                return _isEU;
            }
            set
            {
                _isEU = value;
                OnPropertyChanged(nameof(isEU));
            }
        }


        public HeaderEnvironment()
        {
            Headers = new Dictionary<string, string>();

            SQLDBInfo = new SQLDBInfo();

        }

        public string GetNameProperty(string propertyName)
        {
            string propName = string.Empty;

            PropertyInfo pi = typeof(HeaderEnvironment).GetProperty(propertyName);
            propName = pi.Name;
            return propName;
        }

        public string GetNameMapping(Mappings mapping)
        {
            return mapping.Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
