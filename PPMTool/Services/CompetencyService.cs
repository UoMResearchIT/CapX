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
    }
}
