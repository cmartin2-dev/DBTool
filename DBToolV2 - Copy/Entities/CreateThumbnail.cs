using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CreateThumbnail
    {
        public string TaskId { get; set; }
        public int Sequence { get; set; }

        public CustomData[] CustomData { get; set; }
    }

    public class CustomData
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
