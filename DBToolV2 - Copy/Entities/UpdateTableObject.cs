using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class UpdateTableObject
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
        public object PrimaryKeyValue { get; set; } 

        public string PrimaryKeyDataType { get; set; }

        public string PrimaryKeyValueScript { get; set; }   
        public List<UpdateFieldsObject> updateFieldsObjects { get; set; }

        public UpdateTableObject()
        {
            updateFieldsObjects = new List<UpdateFieldsObject>();
        }

    }

}
