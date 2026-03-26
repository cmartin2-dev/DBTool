using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class UpdateInfo
    {
        public string version { get; set; }
        public string downloadUrl { get; set; }

        public string changelog { get; set; }

       public string changelogdDir { get; set; }    
    }
}
