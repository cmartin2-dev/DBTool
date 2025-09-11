using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustObj
    {
        public int PKTempId { get; set; }
        public IDictionary<string, object> Fields { get; set; }

        public List<CustomObject> Objects { get; set; }
    }
}
