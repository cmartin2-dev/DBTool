using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustomerDataGrowth : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private string folderPath;
        private int day;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Name"));
            }
        }
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Description"));
            }
        }
        public string FolderPath
        {
            get
            {
                return folderPath;
            }
            set
            {
                folderPath = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("folderPath"));
            }
        }

        public int Day
        {
            get
            {
                return day;
            }
            set
            {
                day = value;
                InvokePropertyChanged(new PropertyChangedEventArgs("Day"));
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
