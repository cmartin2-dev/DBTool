using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class QueryLog : INotifyPropertyChanged
    {

        string _JIRATicket;
        string _DescriptionOfScript;
        string _ReleaseVersion;
        string _DateExecuted;
        string _TenantId;
        string _SchemaVersion;
        string _Script;
        string _Status;

        public string JIRATicket
        {
            get
            {
                return _JIRATicket;
            }

            set
            {
               _JIRATicket = value;
                OnPropertyChanged(nameof(JIRATicket));
            }
        }
        public string DescriptionOfScript
        {
            get
            {
                return _DescriptionOfScript;
            }

            set
            {
                _DescriptionOfScript = value;
                OnPropertyChanged(nameof(DescriptionOfScript));
            }
        }
        public string ReleaseVersion
        {
            get
            {
                return _ReleaseVersion;
            }

            set
            {
                _ReleaseVersion = value;
                OnPropertyChanged(nameof(ReleaseVersion));
            }
        }
        public string DateExecuted
        {
            get
            {
                return _DateExecuted;
            }

            set
            {
                _DateExecuted = value;
                OnPropertyChanged(nameof(DateExecuted));
            }
        }
        public string TenantId
        {
            get
            {
                return _TenantId;
            }

            set
            {
                _TenantId = value;
                OnPropertyChanged(nameof(TenantId));
            }
        }
        public string SchemaVersion
        {
            get
            {
                return _SchemaVersion;
            }

            set
            {
                _SchemaVersion = value;
                OnPropertyChanged(nameof(SchemaVersion));
            }
        }
        public string Script
        {
            get
            {
                return _Script;
            }

            set
            {
                _Script = value;
                OnPropertyChanged(nameof(Script));
            }
        }
        public string Status
        {
            get
            {
                return _Status;
            }

            set
            {
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }


        //OnPropertyChanged(nameof(ExcludedKeys));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
