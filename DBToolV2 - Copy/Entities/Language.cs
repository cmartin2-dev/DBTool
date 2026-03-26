using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Entities
{
    public class Language : INotifyPropertyChanged, ICloneable
    {
        private string _Name;
        private string _Culture;
        private bool _isSelected;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public string Culture
        {
            get
            {
                return _Culture;
            }
            set
            {
                _Culture = value;
                OnPropertyChanged(nameof(Culture));
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                // InvokePropertyChanged(new PropertyChangedEventArgs("IsSelected"));

            }
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
