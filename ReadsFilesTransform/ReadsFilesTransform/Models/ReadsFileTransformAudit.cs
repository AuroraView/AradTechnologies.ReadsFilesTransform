using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReadsFilesTransform.Models
{
    [Table("ReadsFileTransformAudit")]  // Ensures EF uses the exact table name
        public class ReadsFileTransformAudit
    {
        [Key]  // Marks it as the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Identity auto-increment
        public int Id { get; set; }
        public string SourceCompany { get; set; }
        public DateTime ProcessingTime { get; set; }
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public string ErrortFileName { get; set; }
        public int SuccessRecordsCount { get; set; }
        public int ErrRecordsCount { get; set; }

    }
}
