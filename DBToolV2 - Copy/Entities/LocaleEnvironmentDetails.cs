using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class LocaleEnvironmentDetails
    {
        public Region Region { get; set; }

        public RegionTenant RegionTenant{ get; set; }

        public string Schema { get; set; }
    }
}
