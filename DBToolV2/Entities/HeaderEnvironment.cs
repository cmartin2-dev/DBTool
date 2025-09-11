using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class HeaderEnvironment : INotifyPropertyChanged
    {

        private string _envName = string.Empty;
        public int Id { get; set; }
        
        public string EnvironmentName { get { 
            
                if(string.IsNullOrEmpty(_envName))
                {
                    _envName = TenantName; 
                }
                return _envName;
            } set
            {
                _envName = value;
            }
        }
        public string TenantName { get; set; }
        public Dictionary<string, string> Headers { get; set; }


        public string EndPoint { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Resource { get; set; }

        public string TokenUrl { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }

        public string Token { get; set; }


        public bool isOAuth1 { get; set; }

        public SQLDBInfo SQLDBInfo { get; set; }

        public string DefaultFSH { get; set; }

        public string IDMTask { get; set; }
        public string ThumbnailTask { get; set; }

        public string IONAPI { get; set; }

        public string TenantList { get; set; }

        public bool GetDataGrowth { get; set; }

        public bool isUS { get; set; }
        public bool isEU { get; set; }


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

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }
}
