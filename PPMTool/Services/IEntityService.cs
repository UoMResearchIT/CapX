using System.Collections;
using PPMTool.Data.Context;

namespace PPMTool.Services
{
    public interface IEntityService<T>
    {
        public abstract int Add(PPMToolContext context, T entity);
        public IEnumerable GetAll(PPMToolContext context);
        public int Update(PPMToolContext context, T entity);
        public void Delete(PPMToolContext context, T entity);
        public void RestoreModel<U>(PPMToolContext context, ref U entity);

    }
}
