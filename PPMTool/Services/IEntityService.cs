using System.Collections;
using PPMTool.Data;

namespace PPMTool.Services
{
    public interface IEntityService<T>
    {
        public abstract int Add(PPMToolContext context, T entity);
        public IEnumerable GetAll(PPMToolContext context);
        public void Update(PPMToolContext context, T entity);
        public void Delete(PPMToolContext context, T entity);
    }
}
