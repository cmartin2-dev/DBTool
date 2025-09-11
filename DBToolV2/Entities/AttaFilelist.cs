using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class AttaFilelist
    {
        public int attafilelistid { get; set; }
        public string cfilename { get; set; }
        public string code { get; set; }
        public int referenceid { get; set; }

        public long FileSize { get; set; }

        public string cfilename_nopath { get; set; }

        public string newfilename { get; set; }

        public bool isUploaded { get; set; }

        public bool isThumbnailCreated { get; set; }

        public int processNo { get; set; }

        public string errorMsg { get; set; }
    }
}
