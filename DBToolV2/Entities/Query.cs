using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Query : INotifyPropertyChanged, ICloneable
    {
        public Query() { }


        public object Clone()
        {
            return MemberwiseClone();
        }

        private bool _isUser = false;
        private string _Name;

        private string _Description;
        private string _QueryString;

        private bool _IsPrivate;

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

        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
        public string QueryString
        {
            get
            {
                return _QueryString;
            }
            set
            {
                _QueryString = value;
                OnPropertyChanged(nameof(QueryString));
            }

        }

        public bool IsPrivate { 
            get
            {
                return _IsPrivate;
            }
            set
            {
                _IsPrivate = value;
                OnPropertyChanged(nameof(IsPrivate));
            }
        }

        public bool IsUser
        {
            get { return _isUser; }
            set { _isUser = value;

                OnPropertyChanged(nameof(IsUser));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
