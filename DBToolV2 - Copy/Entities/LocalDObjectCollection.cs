using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class LocalDObjectCollection<T> : localObjectCollectionBase
    {
        public LocalDObjectCollection()
        {

        }

        public static LocalDObjectCollection<localObjectBase> ConvertList(List<localObjectBase> localObjects)
        {
            LocalDObjectCollection<localObjectBase> localObjectBases = new LocalDObjectCollection<localObjectBase>();
            foreach (localObjectBase localObject in localObjects)
            {
                localObjectBases.Add(localObject);
            }

            return localObjectBases;
        }
    }
}
