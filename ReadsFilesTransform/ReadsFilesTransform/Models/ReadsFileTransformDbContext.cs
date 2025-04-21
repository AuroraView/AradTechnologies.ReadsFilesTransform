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
        public ReadsFileTransformDbContext(DbContextOptions<ReadsFileTransformDbContext> options) : base(options) { }
        public DbSet<Properties> Properties { get; set; }
        public DbSet<ReadsFileTransformAudit> ReadsFileTransformAudit { get; set; }
    }
}