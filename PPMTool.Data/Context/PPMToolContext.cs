// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Entities;

namespace PPMTool.Data.Context
{
    public class PPMToolContext : DbContext
    {
        public DbSet<Absence> Absence { get; set; } = null!;
        public DbSet<InnateCode> InnateCodes { get; set; } = null!;
        public DbSet<InnateCodeTask> InnateCodeTasks { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Person> People { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<SubTask> SubTasks { get; set; } = null!;
        public DbSet<Timesheet> Timesheets { get; set; } = null!;
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; } = null!;
        public DbSet<Resource> Resources { get; set; } = null!;
        public DbSet<SkillTag> SkillTags { get; set; } = null!;
        public DbSet<OwnedSkill> OwnedSkills { get; set; } = null!;
        public DbSet<WorkloadModelChange> WorkloadModelChanges { get; set; } = null!;
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<FinancialReference> FinancialReferences { get; set; } = null!;
        public DbSet<FinancialReferenceValue> FinancialReferenceValues { get; set; } = null!;
        public DbSet<FinancialReferenceValueSet> FinancialReferenceValueSets { get; set; } = null!;
        public DbSet<Competency> Competencies { get; set; } = null!;
        public DbSet<CompetencyAssessment> CompetencyAssessments { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<FundingSource> FundingSources { get; set; } = null!;
        public DbSet<School> Schools { get; set; } = null!;
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        public DbSet<Setting> Settings { get; set; } = null!;

        /// <summary>
        /// Inject options.
        /// </summary>
        /// <param name="options"></param>
        public PPMToolContext(DbContextOptions<PPMToolContext> options)
            : base(options)
        {
            Debug.WriteLine($"** {ContextId} context created.");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PPMToolContext()
        {
        }

        /// <summary>
        /// Override to configure context
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Need to add explicit FKs for all navigation properties to avoid shadw properties being created for this to work
            //optionsBuilder.UseLazyLoadingProxies();
        }

        /// <summary>
        /// Define the model.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WorkloadModelChange>()
                .HasOne(x => x.CostValueSet)
                .WithMany()
                .HasForeignKey(x => x.CostValueSetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FinancialReferenceValue>()
                .HasOne(x => x.FinancialReferenceValueSet)
                .WithMany()
                .HasForeignKey(x => x.FinancialReferenceValueSetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Npgsql v6+ maps DateTime -> timestamptz and rejects Kind=Unspecified.
            // The codebase uses DateTime.Today/DateTime.Now throughout, which produce
            // Kind=Unspecified. Map all DateTime columns to timestamp without time zone
            // so Npgsql treats them as local/unspecified rather than requiring UTC.
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var property in entityType.GetProperties()
                        .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
                    {
                        property.SetColumnType("timestamp without time zone");
                    }
                }
            }
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        public override void Dispose()
        {
            Debug.WriteLine($"** {ContextId} context disposed.");
            base.Dispose();
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/></returns>
        public override ValueTask DisposeAsync()
        {
            Debug.WriteLine($"** {ContextId} context disposed async.");
            return base.DisposeAsync();
        }
    }
}
