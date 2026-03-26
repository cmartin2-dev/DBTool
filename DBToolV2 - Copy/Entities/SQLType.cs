using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class SQLType
    {
        public SQLType() { }

        public int Id { get; set; }
        public string Name { get; set; }
        public string SQLDataType { get; set; }
        public string CSharpDataType { get; set; }

        public bool IsNullable { get; set; }
    }
}
