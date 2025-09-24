using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class SQLDBInfo : INotifyPropertyChanged
    {
        private string _Server;
        private string _Username;
        private string _Password;

        private bool _IsWindowsAuthentication;


        public string Server
        {

            get
            { return _Server; }
            set
            {
                _Server = value;
                OnPropertyChanged(nameof(Server));

            }
        }
        public string Username
        {
            get
            { return _Username; }
            set
            {
                _Username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
        public string Password
        {
            get
            { return _Password; }
            set
            {
                _Password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public bool IsWindowsAuthentication
        {
            get
            { return _IsWindowsAuthentication; }
            set
            {
                _IsWindowsAuthentication = value;
                OnPropertyChanged(nameof(IsWindowsAuthentication));
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
