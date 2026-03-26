using System;
using System.Collections.Generic;
using System.IO.Compression;

namespace Entities
{
    public class ArchiveType
    {
        public int Id { get; set; } 
        public string Module { get; set; }  
        public string Name { get; set; }    
        public ZipArchiveEntry ZipArchiveEntry { get; set; }    
    }
}
