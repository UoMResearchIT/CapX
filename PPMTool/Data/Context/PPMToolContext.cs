using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Entities;

namespace PPMTool.Data.Context
{
    public class PPMToolContext : DbContext
    {
        public DbSet<Absence> Absence { get; set; }
        public DbSet<ActivityTimeRecord> ActivityTimeRecords { get; set; }
        public DbSet<InnateCode> InnateCodes { get; set; }
        public DbSet<InnateCodeTask> InnateCodeTasks { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<SubTask> SubTasks { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<TimesheetWorkflow> TimesheetWorkflows { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<SkillTag> SkillTags { get; set; }
        public DbSet<WorkloadModelChange> WorkloadModelChanges { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<FinancialReference> FinancialReferences { get; set; }
        public DbSet<Competency> Competencies { get; set; }
        public DbSet<CompetencyAssessment> CompetencyAssessments { get; set; }

        /// <summary>
        /// Inject options.
        /// </summary>
        /// <param name="options"></>
        /// for the context
        /// </param>
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
        }

        /// <summary>
        /// Define the model.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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
