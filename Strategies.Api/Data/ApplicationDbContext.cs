using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<VisaCategory> VisaCategories { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<ChecklistTemplate> ChecklistTemplates { get; set; }
        public DbSet<CaseStageHistory> CaseStageHistories { get; set; }
        public DbSet<VisaRecord> VisaRecords { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AgentRun> AgentRuns { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Lekabe will add relationships and foreign keys here in Phase 2
        }
    }
}