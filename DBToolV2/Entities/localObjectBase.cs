using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class localObjectBase : LocalDynamicObject
    {
        public int Id { get; set; }
        public string Name { get; set; }


        public LocalDObject DynamicObject { get; set; }

        public MethodInfo MethodInfo { get; set; }
    }
}
