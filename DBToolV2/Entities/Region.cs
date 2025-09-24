using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Region : INotifyPropertyChanged, ICloneable
    {

        public Region()
        {
            Headers = new ObservableCollection<Header>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        private int id;
        private string regionName;
        private string regionEndpoint;
        private string regionClientId;
        private string regionClientSecret;
        private HeaderEnvironment headerEnvironment;
        private int headerEnvironmentId;
        private string regionUpgradePath;
        private string regionUpgradeBody;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Id"));
            }
        }
        public string RegionName
        {
            get
            {
                return regionName;
            }
            set
            {
                regionName = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionName"));
            }
        }
        public string RegionEndPoint
        {
            get
            {
                return regionEndpoint;
            }
            set
            {
                regionEndpoint = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionEndPoint"));
            }
        }
        public string RegionClientId
        {
            get
            {
                return regionClientId;
            }
            set
            {
                regionClientId = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionClientId"));
            }
        }
        public string RegionClientSecret
        {
            get
            {
                return regionClientSecret;
            }
            set
            {
                regionClientSecret = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionClientSecret"));
            }
        }

        //public HeaderEnvironment HeaderEnvironment
        //{
        //    get
        //    {
        //        return headerEnvironment;
        //    }
        //    set
        //    {
        //        headerEnvironment = value;

        //    }
        //}

        public string RegionUpgradePath
        {
            get
            {
                return regionUpgradePath;
            }
            set
            {
                regionUpgradePath = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionUpgradePath"));
            }
        }

        public string RegionUpgradeBody
        {
            get
            {
                return regionUpgradeBody;
            }
            set
            {
                regionUpgradeBody = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("RegionUpgradeBody"));
            }
        }


        public ObservableCollection<Header> Headers { get; set; }

        public int HeaderEnvironmentId
        {
            get
            {

                return headerEnvironmentId;
            }
            set
            {
                headerEnvironmentId = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("HeaderEnvironmentId"));

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }

    public class RegionTenant
    {
        private string _tenantId;
        private string _status;
        private string _dbVersion;

        private Region _Region;

        private string _TenantEnvironmentName;

        public RegionTenant()
        {
            _tenantId = "Tenant Name";
        }


        public string tenantId
        {
            get
            {
                return _tenantId;
            }
            set
            {
                _tenantId = value;

                InvokePropertyChanged(new PropertyChangedEventArgs("tenantId"));
            }
        }
        public string status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("status"));

            }
        }
        public string dbVersion
        {
            get
            {
                return _dbVersion;
            }
            set
            {
                _dbVersion = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("dbVersion"));
            }
        }

        public Region Region
        {
            get
            { return _Region; }
            set
            {
                _Region = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Region"));
            }

        }

        public string TenantEnvironmentName
        {
            get
            {
                return _TenantEnvironmentName;
            }
            set
            {
                _TenantEnvironmentName = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("TenantEnvironmentName"));

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }
}
