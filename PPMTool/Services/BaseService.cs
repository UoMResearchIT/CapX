using System.Collections;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;

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
        public void RestoreModel<U>(PPMToolContext context, ref U entity)
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
        public abstract IEnumerable GetAll(PPMToolContext context);
        public abstract void Update(PPMToolContext context, T entity);
    }
}
