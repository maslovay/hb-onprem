using System.Collections.Generic;

namespace CloneFtpOnAzureService
{
    public class StorageAccInfo
    {
        public string AccName { get; set; }
        
        public string AccKey { get; set; }
        
        public string[] DirectoryName { get; set; }
        
        public string VideoName { get; set; }
        
        public string AudioName { get; set; }
        
        public string AvatarName { get; set; }
    }
}