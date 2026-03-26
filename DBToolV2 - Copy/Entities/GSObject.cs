using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class GSObject
    {
        public SettingsObject Settings { get; set; }
        public string Version {  get; set; }

        public string SaveLocation { get; set; }
    }
}
