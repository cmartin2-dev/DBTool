using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class RequestResponse
    {
        public bool isSuccess { get; set; }
        public string StatusMessage { get; set; }
        public string ErrorMessage { get; set; }

        public int RowCount { get; set; }

        public CustObj CustObj { get; set; }

        public DataSet DataSet { get; set; }

        public List<RegionTenant> TenantList { get; set; }

        public void SetErrorResponse(string message)
        {
            this.StatusMessage = message;
            this.ErrorMessage = message;
            this.isSuccess = false;
        }
    }
}
