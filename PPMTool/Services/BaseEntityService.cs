using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;

namespace PPMTool.Services
{
    public abstract class BaseEntityService<T> : IEntityService<T>
    {
        /// <summary>
        /// Method to restore a model to its unmodified state in the database after local modification.
        /// Doesn't have to be the same type as the service that extends this class 
        /// (not sure that satisfies separation of conerns though!)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        public virtual void RestoreModel<U>(PPMToolContext context, ref U entity)
        {
            var resEntry = context.Entry(entity);
            if (resEntry.State == EntityState.Modified)
            {
                resEntry.CurrentValues.SetValues(resEntry.OriginalValues);
                resEntry.State = EntityState.Unchanged;
            }
        }

        public abstract int Add(PPMToolContext context, T entity, bool commitChanges = true);
        public abstract void Delete(PPMToolContext context, T entity, bool commitChanges = true);
        public abstract IEnumerable<T> GetAll(PPMToolContext context);
        public abstract int Update(PPMToolContext context, T entity, bool commitChanges = true);

        /// <summary>
        /// Method to allow services to define their own definition of a duplicate
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual bool DuplicateDetected(PPMToolContext context, T entity)
        {
            return false;
        }

        /// <summary>
        /// Method to generate a list of differences between the original and current values of an entity
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IList<EntityDiff<U>> GetDiffList<U>(PPMToolContext context) where U : class
        {
            // Initialise
            var diffList = new List<EntityDiff<U>>();

            // Check entity state
            var modifiedEntities = context.ChangeTracker.Entries<U>()
                .Where(p => p.State == EntityState.Modified || p.State == EntityState.Added || p.State == EntityState.Deleted).ToList();

            // Loop over changes
            foreach (var change in modifiedEntities)
            {
                // For every property, create a diff entry
                foreach (var prop in change.OriginalValues.Properties)
                {
                    var originalValue = change.OriginalValues[prop]?.ToString() ?? null;
                    var currentValue = change.CurrentValues[prop]?.ToString() ?? null;

                    // Record the diff of modified properties or all properties if an add or delete
                    if (originalValue != currentValue || change.State == EntityState.Added || change.State == EntityState.Deleted)
                    {
                        diffList.Add(new EntityDiff<U>(change.Entity, change.State, prop.Name, originalValue, currentValue));
                        Debug.WriteLine($"** ID:{change.Entity} | {change.State} | {originalValue} -> {currentValue}");
                    }
                }
            }

            return diffList;
        }

        /// <summary>
        /// Write any staged changes to the database
        /// </summary>
        /// <param name="context"></param>
        public virtual void CommitChanges(PPMToolContext context)
        {
            context.SaveChanges();
        }
    }
}
