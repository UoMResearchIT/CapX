using System.Collections.Generic;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public interface IEntityService<T> where T : IEntity
    {
        public abstract int Add(PPMToolContext context, T entity);
        public IEnumerable<T> GetAll(PPMToolContext context);
        public int Update(PPMToolContext context, T entity);
        public void Delete(PPMToolContext context, T entity);
        public void RestoreModel<U>(PPMToolContext context, ref U entity);

    }
}
