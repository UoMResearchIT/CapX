using PPMTool.Data;
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
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceId;
        }

        public override void Delete(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            context.FinancialReferences.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Returns the Financial References from the db. If none have been added then the app will
        /// crash in certain places if the Finance Feature is not enabled. The check in the method
        /// helps avoid this exception by passing back a non-null, non-zero IEnumerable to satisfy
        /// the requesting call. This was primarily added to bypass the crash when a new Project
        /// was added without the Finance Feature being enabled.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<FinancialReference> GetAll(PPMToolContext context)
        {
            if (!context.FinancialReferences.Any())
            {
                return new List<FinancialReference> { new FinancialReference() };
            }
            return context.FinancialReferences;
        }

        public override int Update(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.FinancialReferences.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceId;
        }

        public override bool DuplicateDetected(PPMToolContext context, FinancialReference entity)
        {
            return context.FinancialReferences.Any(x => x.FinancialYear == entity.FinancialYear && x.FinancialReferenceId != entity.FinancialReferenceId);
        }

        /// <summary>
        /// Method to return a suitable financial reference following set logic given a date in a certain financial year
        /// </summary>
        /// <param name="context"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public FinancialReference GetFinancialReferenceForDate(PPMToolContext context, DateTime startDate)
        {
            return GetAll(context).GetSuitableFinancialReference(startDate);
        }
    }
}
