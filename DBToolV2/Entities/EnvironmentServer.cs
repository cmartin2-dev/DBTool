using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class EnvironmentServer : ICloneable, INotifyPropertyChanged
    {

        private int _id;
        private string _serverName;

        public object Clone()
        {
            return MemberwiseClone();
        }
        public EnvironmentServer()
        {
            Databases = new ObservableCollection<EnvironmentDatabase>();
        }

        public int Id { 
        get
            {  return _id; }
            set { _id = value;

                InvokePropertyChanged(new PropertyChangedEventArgs("Id"));
            }
        }

        public string ServerName { 
        get { return _serverName; }
            set
            {
                _serverName = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("ServerName"));
            }
        }

        public ObservableCollection<EnvironmentDatabase> Databases { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }
}
