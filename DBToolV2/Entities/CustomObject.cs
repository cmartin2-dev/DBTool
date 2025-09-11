using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustomObject
    {
        public int PKId { get; set; }

        public IDictionary<string, object> Object { get; set; }

        public CustomObject() {
        
            Object= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
