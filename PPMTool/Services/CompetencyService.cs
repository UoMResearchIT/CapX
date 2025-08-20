using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class CompetencyService : BaseEntityService<Competency>
    {
        /// <inheritdoc />
        public override int Add(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Competencies.Add(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return entity.CompetencyId;
        }

        /// <summary>
        /// We probably don't ever want to delete a competency as it would be messy if associated with past competency 
        /// assessments so it ought to be a soft delete by setting the <see cref="Competency.IsActive"> property to false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        public override void Delete(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            entity.IsActive = false;
            Update(context, entity);
        }

        /// <summary>
        /// Return all the competencies in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Competency> GetAll(PPMToolContext context)
        {
            return context.Competencies
                .Include(x => x.Assessments);
        }

        /// <summary>
        /// Return all the competencies in the DB and asynchronously copies them to memory
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Competency>> GetAllAsync(PPMToolContext context)
        {
            return await context.Competencies
                .Include(x => x.Assessments)
                .ToListAsync();
        }

        /// <summary>
        /// Return only active competencies
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Competency>> GetAllActiveAsync(PPMToolContext context)
        {
            var allCompetencies = await GetAllAsync(context);
            return allCompetencies
                .Where(x => x.IsActive);
        }

        /// <inheritdoc />
        public override int Update(PPMToolContext context, Competency entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }
            context.Competencies.Update(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return entity.CompetencyId;
        }

        /// <summary>
        /// Return a competency by its ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="competencyId"></param>
        /// <returns></returns>
        public Competency GetById(PPMToolContext context, int competencyId)
        {
            return context.Competencies.FirstOrDefault(x => x.CompetencyId == competencyId);
        }

        /// <summary>
        /// Duplicate detected based on legacy ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Competency entity)
        {
            if (string.IsNullOrEmpty(entity?.LegacyId))
            {
                return false;
            }
            var legacyId = entity?.LegacyId.Trim().ToLower();
            return context.Competencies.Any(x => x.CompetencyId != entity.CompetencyId && x.LegacyId.Trim().ToLower() == legacyId);
        }

        /// <summary>
        /// Update a competency assessment
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int UpdateAssessment(PPMToolContext context, CompetencyAssessment entity, bool commitChanges = true)
        {
            context.CompetencyAssessments.Update(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return entity.CompetencyAssessmentId;
        }

        /// <summary>
        /// Add a new competency assessment
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int AddAssessment(PPMToolContext context, CompetencyAssessment entity, bool commitChanges = true)
        {
            context.CompetencyAssessments.Add(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return entity.CompetencyAssessmentId;
        }
    }
}
