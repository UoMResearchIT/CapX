using System.Collections.Generic;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class FinancialReferenceService : BaseEntityService<FinancialReference>
    {
        public override int Add(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.FinancialReferences.Add(entity);
            if (commitChanges) context.SaveChanges();
            return entity.FinancialReferenceId;
        }

        public override void Delete(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            context.FinancialReferences.Remove(entity);
            if (commitChanges) context.SaveChanges();
        }

        public override IEnumerable<FinancialReference> GetAll(PPMToolContext context)
        {
            return context.FinancialReferences;
        }

        public override int Update(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.FinancialReferences.Update(entity);
            if (commitChanges) context.SaveChanges();
            return entity.FinancialReferenceId;
        }
    }
}
