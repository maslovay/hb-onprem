using System;

namespace UserOperations.Models
{
    public class FileFramePostModel
    {
        public double? Age { get; set; }
        public string Gender { get; set; }
        public double? Yaw { get; set; }
        public double? Smile { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public Guid? DeviceId { get; set; }
        public DateTime Time { get; set; }
        public string Descriptor { get; set; }
        public double? FaceArea { get; set; }
    }
}