using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;

namespace PPMTool.Services
{
    public abstract class BaseService<T> : IEntityService<T>
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

        public abstract int Add(PPMToolContext context, T entity);
        public abstract void Delete(PPMToolContext context, T entity);
        public abstract IEnumerable<T> GetAll(PPMToolContext context);
        public abstract int Update(PPMToolContext context, T entity);

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
        /// Class to encapsulate a change to an entity with values represented as strings
        /// </summary>
        internal class EntityDiff
        {
            public int EntityId { get; }
            public EntityState State { get; }
            public string PropertyName { get; }
            public string OriginalValue { get; }
            public string CurrentValue { get; }

            public EntityDiff(int entityId, EntityState state, string propertyName, string originalValue, string currentValue)
            {
                EntityId = entityId;
                State = state;
                PropertyName = propertyName;
                OriginalValue = originalValue;
                CurrentValue = currentValue;
            }
        }
    }
}
