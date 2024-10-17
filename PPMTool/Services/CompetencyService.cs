using System.Collections.Generic;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class CompetencyService : BaseEntityService<Competency>
    {
        public override int Add(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// We probably don't ever want to delete a competency as it would be messy if associated with past competency 
        /// assessments so it ought to be a soft delete by setting the <see cref="Competency.IsActive"> property to false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Delete(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<Competency> GetAll(PPMToolContext context)
        {
            throw new System.NotImplementedException();
        }

        public override int Update(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            throw new System.NotImplementedException();
        }

        internal Competency GetById(PPMToolContext context, int competencyId)
        {
            throw new System.NotImplementedException();
        }
    }
}
