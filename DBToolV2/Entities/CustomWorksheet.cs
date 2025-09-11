using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustomWorksheet
    {
        public CustomWorksheet()
        {
            WorkSheetColumns = new List<CustomWorksheetColumn>();
        }

        public int Id { get; set; }

        public string WorkSheetName { get; set; }

        public List<CustomWorksheetColumn> WorkSheetColumns { get; set; }

    }
    public class CustomWorksheetColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string MappedName { get; set; }
    }
}
