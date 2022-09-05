using IceSync.Data.Configuration;
using IceSync.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IceSync.Data
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Workflow> Workflows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new WorkflowConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}