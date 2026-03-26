using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class UpdateFieldsObject
    {

        public string ColumnName { get; set; }
        public object Value { get; set; }

        public string ObjectType { get; set; }

        public string scriptValue { get; set; } 

    }
}
