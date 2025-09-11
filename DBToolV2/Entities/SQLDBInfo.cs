using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class SQLDBInfo
    {
        public string Server { get; set; }  
        public string Username { get; set; }
        public string Password { get; set; }

        public bool IsWindowsAuthentication { get; set; }
    }
}
