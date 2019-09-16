using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HBData.Models
{
    /// <summary>
    ///     Table for saving banchmarks
    /// </summary>
    public class Benchmark
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     Date (one day from 00:00)
        /// </summary>
        public DateTime Day { get; set; }

        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        public Guid? IndustryId { get; set; }
        public CompanyIndustry Industry { get; set; }

        public Guid BenchmarkNameId { get; set; }
        public BenchmarkName BenchmarkName { get; set; }

        public double Value { get; set; }

        public double Weight { get; set; }
    }
}