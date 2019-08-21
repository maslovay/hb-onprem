using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about frame attribute
    /// </summary>
    public class VideoFace
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid VideoFaceId { get; set; }

        /// <summary>
        ///     Video id
        /// </summary>
        public Guid FileVideoId { get; set; }
        [JsonIgnore]
        public FileVideo FileVideo { get; set; }

        /// <summary>
        ///     List of faces in video
        /// </summary>
        public String FaceId { get; set; }
    }
}