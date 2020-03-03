using System;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    public class BenchmarkName
    {
        [Key]
        public Guid Id { get; set; }        
        public string Name { get; set; }
    }
}