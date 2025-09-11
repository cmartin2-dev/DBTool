using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class ImageMigrationSettings
    {
        public string Tenant { get; set; }
        public HeaderEnvironment HeeaderEnvironment { get; set; }

        public string APIString { get; set; }
        public string Schema { get; set; }
        public int NumberOfProcess { get; set; }
        public string MasterFile { get; set; }
        public string ParentUploadFolder { get; set; }
        public string SourceAttachmentFolder { get; set; }
        public string ForUploadAttachmentFolder { get; set; }
        public string LogFolder { get; set; }
        public string ErrorLogFoder { get; set; }
        public string ReferenceFile { get; set; }
        public string MainImageFolderLocation { get; set; }

        public string DestinationImageFolder { get; set; }
        public string ReferenceImageFileJSON { get; set; }


    }
}
    