using System.Collections;
using System.Linq;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class InnateCodeService : BaseService<InnateCode>
    {
        /// <summary>
        /// Will not add a duplicate but return -1 instead. If successfully added, will return new ID of saved entity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, InnateCode entity)
        {
            if (context.InnateCodes.FirstOrDefault(x => x.GetCodeAsString().ToLower() == entity.GetCodeAsString().ToLower()) != null)
            {
                // Duplicate found!
                return -1;
            }

            context.InnateCodes.Add(entity);
            context.SaveChanges();
            return entity.InnateCodeId;
        }

        public override void Delete(PPMToolContext context, InnateCode entity)
        {
            context.InnateCodes.Remove(entity);
            context.SaveChanges();
        }

        public override IEnumerable GetAll(PPMToolContext context)
        {
            return context.InnateCodes.ToList();
        }

        public override void Update(PPMToolContext context, InnateCode entity)
        {
            context.InnateCodes.Update(entity);
            context.SaveChanges();
        }
    }
}
