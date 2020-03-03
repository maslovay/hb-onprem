using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HBData.Models
{
    /// <summary>
    ///      Record customer frames information
    /// </summary>
    public class ClientSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ClientSessionId { get; set; }
        public DateTime Time { get; set; }
        public Guid ClientId { get; set; }
        public Client Client { get; set; }
        public string FileName { get; set; }
    }
}