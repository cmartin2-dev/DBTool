using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Region : INotifyPropertyChanged
    {

        public Region() {
            Headers = new List<Header>();
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
                regionClientId= value;
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


        public List<Header> Headers { get; set; }

        public int HeaderEnvironmentId
        {
            get
            {
             
                return headerEnvironmentId;
            }
            set
            {
                headerEnvironmentId = value;

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
        public string tenantId { get; set; }
        public string status { get; set; }
        public string dbVersion { get; set; }

        public Region Region { get; set; }

        public string TenantEnvironmentName { get; set; }
    }
}
