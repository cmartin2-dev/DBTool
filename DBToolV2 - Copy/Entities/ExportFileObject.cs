using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class ExportFileObject
    {
        public string TableName { get; set; }
        public string JSON { get; set; }
        public DataTable DataTable { get; set; }
    }
}
