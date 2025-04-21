using Microsoft.EntityFrameworkCore;
using ReadsFilesTransform.Models;

namespace ReadsFilesTransform
{
    /// <summary>
    /// DbContext and DbEntities definitions
    /// </summary>
    /// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
    public class ReadsFileTransformDbContext : DbContext
    {
        /// <summary>Initializes a new instance of the <see cref="ReadsFileTransformDbContext" /> class.</summary>
        /// <param name="options">The options.</param>
        public ReadsFileTransformDbContext(DbContextOptions<ReadsFileTransformDbContext> options) : base(options) { }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public DbSet<Properties> Properties { get; set; }

        /// <summary>
        /// Gets or sets the reads file transform audit.
        /// </summary>
        /// <value>
        /// The reads file transform audit.
        /// </value>
        public DbSet<ReadsFileTransformAudit> ReadsFileTransformAudit { get; set; }
    }
}