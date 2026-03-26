using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class VersionChangeset
    {
        public VersionChangeset()
        {
            ChangeSetItem = new List<ChangesetItem>();
            FileDirectories = new List<string>();
        }

        public int Id { get; set; }

        public string Version { get; set; }

        public List<string> FileDirectories { get; set; }

        public List<ChangesetItem> ChangeSetItem { get; set; }
    }

    public class ChangesetItem
    {
        public string Id { get; set; }
        public string Script { get; set; }
        public string FileName { get; set; }
        public string Author { get; set; }

        public string Comment { get; set; }
        public string Schema { get; set; }
    }
}
