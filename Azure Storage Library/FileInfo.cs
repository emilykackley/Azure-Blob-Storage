using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageLibrary
{
    class FileInfo
    {
        public string BlobName { get; set; }

        public string FileName { get; set; }

        public string BlobStatus { get; set; }

        public string FileComments { get; set; }

        public string UploadDate { get; set; }

        public string DeleteDate { get; set; }

        public string RecoverDate { get; set; }

        public string FileSize { get; set; }

        public string UploadedBy { get; set; }

        public string DeletedBy { get; set; }

        public string RecoveredBy { get; set; }
    }
}
