using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PPMTool.Areas.Identity.Data;

namespace PPMTool.Data
{
    public class PPMToolContext : IdentityDbContext<PPMToolUser>
    {
        public DbSet<Person> People { get; set; }

        /// <summary>
        /// Inject options.
        /// </summary>
        /// <param name="options"></>
        /// for the context
        /// </param>
        public PPMToolContext(DbContextOptions<PPMToolContext> options)
            : base(options)
        {
            Debug.WriteLine($"{ContextId} context created.");
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
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("DataSource=PPMTool.db");

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
            Debug.WriteLine($"{ContextId} context disposed.");
            base.Dispose();
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/></returns>
        public override ValueTask DisposeAsync()
        {
            Debug.WriteLine($"{ContextId} context disposed async.");
            return base.DisposeAsync();
        }
    }
}
