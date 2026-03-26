using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Changeset
    {
        public string Author { get; set; }
        public string Id { get; set; }
        public string Comment { get; set; }
        public List<string> Params { get; set; } = new();
        public string Script { get; set; }
    }
}
