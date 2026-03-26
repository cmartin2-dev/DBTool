using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class LocaleOption
    {
        public bool IsRemoveMnemonic { get; set; }
        public bool IsRemoveExcludedKeys { get;set; }
        public bool IsNewUpdateOnly { get; set; }
        public bool IncludeLength { get; set; }     

    }
}
