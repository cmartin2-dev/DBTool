using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustomQuery
    {

        [Key]
        public int PKTempId { get; set; }
        public IDictionary<string, object> Fields { get; set; }

        public List<CustomObject> Objects { get; set; }
    }
}
