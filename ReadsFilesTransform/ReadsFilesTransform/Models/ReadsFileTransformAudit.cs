using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReadsFilesTransform.Models
{
    [Table("ReadsFileTransformAudit")]
    public class ReadsFileTransformAudit
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the source company.
        /// </summary>
        /// <value>
        /// The source company.
        /// </value>
        public string SourceCompany { get; set; }

        /// <summary>
        /// Gets or sets the processing time.
        /// </summary>
        /// <value>
        /// The processing time.
        /// </value>
        public DateTime ProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets the name of the input file.
        /// </summary>
        /// <value>
        /// The name of the input file.
        /// </value>
        public string InputFileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the output file.
        /// </summary>
        /// <value>
        /// The name of the output file.
        /// </value>
        public string OutputFileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the errort file.
        /// </summary>
        /// <value>
        /// The name of the errort file.
        /// </value>
        public string ErrortFileName { get; set; }

        /// <summary>
        /// Gets or sets the success records count.
        /// </summary>
        /// <value>
        /// The success records count.<
        /// /value>
        public int SuccessRecordsCount { get; set; }

        /// <summary>
        /// Gets or sets the error records count.
        /// </summary>
        /// <value>
        /// The error records count.
        /// </value>
        public int ErrRecordsCount { get; set; }
    }
}
