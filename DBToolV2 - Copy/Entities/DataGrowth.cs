using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class DataGrowth
    {

        public int GROUPID { get; set; }
        public string GROUPNAME { get; set; }
        public string SCHEMA { get; set; }
        public string   TABLENAME { get; set; }
        public int ROWCOUNT { get; set; }
    }
}
