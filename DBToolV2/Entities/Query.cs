using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Query
    {
       public Query() { }

        private bool _isUser = false;
        public string Name { get; set; }

        public string Description { get; set; } 
        public string QueryString { get; set; } 

        public bool IsPrivate { get; set; }

        public bool isUser
        {
            get { return _isUser; }
            set { _isUser = value; }
        }
    }
}
