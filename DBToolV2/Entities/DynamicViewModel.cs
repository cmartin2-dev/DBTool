using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class DynamicViewModel : DynamicObject, INotifyPropertyChanged
    {
        private readonly object _model;
        private readonly Type _type;

        public DynamicViewModel(object model)
        {
            _model = model;
            _type = model.GetType();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var prop = _type.GetProperty(binder.Name);
            if (prop != null)
            {
                result = prop.GetValue(_model);
                return true;
            }

            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var prop = _type.GetProperty(binder.Name);
            if (prop != null)
            {
                prop.SetValue(_model, value);
                OnPropertyChanged(binder.Name);
                return true;
            }
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
