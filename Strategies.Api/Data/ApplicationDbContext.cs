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

            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<VisaCategory>(visaCategory =>
            {
                visaCategory.ToTable("VisaCategory");

                visaCategory.HasKey(category => category.Id);

                visaCategory.Property(category => category.Id)
                    .ValueGeneratedOnAdd();

                visaCategory.Property(category => category.Name)
                    .IsRequired();

                visaCategory.Property(category => category.Description);

                visaCategory.Property(category => category.IsActive)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<DocumentType>(documentType =>
            {
                documentType.ToTable("DocumentType");

                documentType.HasKey(type => type.Id);

                documentType.Property(type => type.Id)
                    .ValueGeneratedOnAdd();

                documentType.Property(type => type.Name);

                documentType.Property(type => type.Description);
            });

            modelBuilder.Entity<User>(user =>
            {
                user.ToTable("User");

                user.HasKey(account => account.Id);

                user.Property(account => account.Id)
                    .ValueGeneratedOnAdd();

                user.Property(account => account.FullName)
                    .IsRequired();

                user.Property(account => account.Email)
                    .IsRequired();

                user.Property(account => account.PasswordHash);

                user.Property(account => account.Role)
                    .IsRequired();

                user.Property(account => account.IsActive)
                    .HasDefaultValue(false);

                user.Property(account => account.EmailConfirmed)
                    .HasDefaultValue(false);

                user.Property(account => account.CreatedAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Lead>(lead =>
            {
                lead.ToTable("Lead");

                lead.HasKey(application => application.Id);

                lead.Property(application => application.Id)
                    .ValueGeneratedOnAdd();

                lead.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(application => application.ReviewedById)
                    .OnDelete(DeleteBehavior.SetNull);

                lead.HasOne<VisaCategory>()
                    .WithMany()
                    .HasForeignKey(application => application.VisaCategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                lead.Property(application => application.NeedsAssistance)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Case>(caseEntity =>
            {
                caseEntity.ToTable("Case");

                caseEntity.HasKey(caseRecord => caseRecord.Id);

                caseEntity.Property(caseRecord => caseRecord.Id)
                    .ValueGeneratedOnAdd();

                caseEntity.HasOne<Lead>()
                    .WithMany()
                    .HasForeignKey(caseRecord => caseRecord.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                caseEntity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(caseRecord => caseRecord.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                caseEntity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(caseRecord => caseRecord.ConsultantId)
                    .OnDelete(DeleteBehavior.SetNull);

                caseEntity.HasOne<VisaCategory>()
                    .WithMany()
                    .HasForeignKey(caseRecord => caseRecord.VisaCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                caseEntity.Property(caseRecord => caseRecord.IsArchived)
                    .HasDefaultValue(false);

                caseEntity.Property(caseRecord => caseRecord.CreatedAt)
                    .HasDefaultValueSql("now()");

                caseEntity.Property(caseRecord => caseRecord.UpdatedAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<CaseStageHistory>(history =>
            {
                history.ToTable("CaseStageHistory");

                history.HasKey(stageHistory => stageHistory.Id);

                history.Property(stageHistory => stageHistory.Id)
                    .ValueGeneratedOnAdd();

                history.HasOne<Case>()
                    .WithMany()
                    .HasForeignKey(stageHistory => stageHistory.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                history.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(stageHistory => stageHistory.ChangedById)
                    .OnDelete(DeleteBehavior.SetNull);

                history.Property(stageHistory => stageHistory.ChangedAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<ChecklistTemplate>(checklist =>
            {
                checklist.ToTable("ChecklistTemplate");

                checklist.HasKey(template => template.Id);

                checklist.Property(template => template.Id)
                    .ValueGeneratedOnAdd();

                checklist.HasOne<VisaCategory>()
                    .WithMany()
                    .HasForeignKey(template => template.VisaCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                checklist.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(template => template.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                checklist.Property(template => template.IsMandatory)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Document>(document =>
            {
                document.ToTable("Document");

                document.HasKey(file => file.Id);

                document.Property(file => file.Id)
                    .ValueGeneratedOnAdd();

                document.HasOne<Case>()
                    .WithMany()
                    .HasForeignKey(file => file.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                document.HasOne<DocumentType>()
                    .WithMany()
                    .HasForeignKey(file => file.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                document.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(file => file.ReviewedById)
                    .OnDelete(DeleteBehavior.SetNull);

                document.Property(file => file.FileName);

                document.Property(file => file.FilePath);

                document.Property(file => file.ReviewComment);

                document.Property(file => file.UploadedAt)
                    .HasDefaultValueSql("now()");

                document.Property(file => file.IsArchived)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Message>(message =>
            {
                message.ToTable("Message");

                message.HasKey(messageRecord => messageRecord.Id);

                message.Property(messageRecord => messageRecord.Id)
                    .ValueGeneratedOnAdd();

                message.HasOne<Case>()
                    .WithMany()
                    .HasForeignKey(messageRecord => messageRecord.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                message.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(messageRecord => messageRecord.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                message.Property(messageRecord => messageRecord.IsRead)
                    .HasDefaultValue(false);

                message.Property(messageRecord => messageRecord.SentAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<AgentRun>(agentRun =>
            {
                agentRun.ToTable("AgentRun");

                agentRun.HasKey(run => run.Id);

                agentRun.Property(run => run.Id)
                    .ValueGeneratedOnAdd();

                agentRun.HasOne<Case>()
                    .WithMany()
                    .HasForeignKey(run => run.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                agentRun.HasOne<Lead>()
                    .WithMany()
                    .HasForeignKey(run => run.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                agentRun.Property(run => run.RanAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<AuditLog>(auditLog =>
            {
                auditLog.ToTable("AuditLog");

                auditLog.HasKey(log => log.Id);

                auditLog.Property(log => log.Id)
                    .ValueGeneratedOnAdd();

                auditLog.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(log => log.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                auditLog.Property(log => log.EntityId)
                    .IsRequired();

                auditLog.Property(log => log.CreatedAt)
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<VisaRecord>(visaRecord =>
            {
                visaRecord.ToTable("VisaRecord");

                visaRecord.HasKey(record => record.Id);

                visaRecord.Property(record => record.Id)
                    .ValueGeneratedOnAdd();

                visaRecord.HasOne<Case>()
                    .WithMany()
                    .HasForeignKey(record => record.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                visaRecord.Property(record => record.RenewalFlagged)
                    .HasDefaultValue(false);
            });
        }
    }
}
