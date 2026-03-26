using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class localObjectCollectionBase : System.Collections.ObjectModel.Collection<localObjectBase>
    {
        public List<string> ColumnHeaders { get; set; }
        public void GetColumnHeader()
        {
            ColumnHeaders = new List<string>();

            if (this.Items.Count > 0)
            {
                localObjectBase obj = this.Items[0] as localObjectBase;
                foreach (var key in obj.Properties)
                {
                    ColumnHeaders.Add(key.Key);
                }
            }
        }

        public List<string> GetColumnHeaderList()
        {
            ColumnHeaders = new List<string>();

            if (this.Items.Count > 0)
            {

                var refItem = this.Items.OrderByDescending(x => x.Properties.Count()).First();

                localObjectBase obj = refItem as localObjectBase;
                foreach (var key in obj.Properties)
                {
                    ColumnHeaders.Add(key.Key);
                }
            }

            return ColumnHeaders;
        }

        public List<string> GetColumnHeaderListIdNameOnly()
        {
            ColumnHeaders = new List<string>();

            if (this.Items.Count > 0)
            {
                var refItem = this.Items.OrderByDescending(x => x.Properties.Count()).First();

                localObjectBase obj = refItem as localObjectBase;
                ColumnHeaders.Add("Id");
                ColumnHeaders.Add("Name");
            }

            return ColumnHeaders;
        }
    }
}
