using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class ExcelReport
    {
        public string Module { get; set; } // index 1
        public string Category { get; set; } // index 5
        public string TransactionName { get; set; }

        public Dictionary<string,string> ColumnDates { get; set; }
    }

    public class ExcelModule
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ExcelDataColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string HeaderName { get; set; }
    }


}
