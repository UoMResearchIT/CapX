using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class SchoolService : BaseEntityService<School>
    {
        /// <summary>
        /// Returns schools in the DB (active ones only, by default)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<School> GetAll(PPMToolContext context)
        {
            return context.Schools;
        }

        /// <summary>
        /// Returns schools in the DB filtered by their IsActive status
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<School> GetAllActive(PPMToolContext context)
        {
            return context.Schools.Where(x => x.IsActive);
        }

        /// <summary>
        /// Updates the school in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="school"></param>
        public override int Update(PPMToolContext context, School school, bool commitChanges = true)
        {
            if (DuplicateDetected(context, school))
            {
                return -1;
            }
            context.Schools.Update(school);
            if (commitChanges) CommitChanges(context);
            return school.SchoolId;
        }

        /// <summary>
        /// Gets the tracking entry for the school
        /// </summary>
        /// <param name="context"></param>
        /// <param name="school"></param>
        /// <returns></returns>
        internal EntityEntry<School> GetEntry(PPMToolContext context, School school)
        {
            return context.Entry(school);
        }

        /// <summary>
        /// Deletes a school from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="school"></param>
        public override void Delete(PPMToolContext context, School school, bool commitChanges = true)
        {
            context.Remove(school);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Adds a new school to the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="school"></param>
        public override int Add(PPMToolContext context, School school, bool commitChanges = true)
        {
            if (DuplicateDetected(context, school))
            {
                return -1;
            }
            context.Add(school);
            if (commitChanges) CommitChanges(context);
            return school.SchoolId;
        }

        /// <summary>
        /// Detect a duplicate school if the name or code is the same as another in the same faculty. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, School entity)
        {
            return GetAll(context)
                .Any(x =>
                    x.SchoolId != entity.SchoolId &&
                    ((x.Name.Trim().ToLower() == entity.Name.Trim().ToLower() || x.Code.Trim().ToLower() == entity.Code.Trim().ToLower())
                        && x.Faculty.FacultyId == entity.Faculty.FacultyId)
                );
        }

        /// <summary>
        /// Get the schools for a given faculty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="facultyId"></param>
        /// <returns></returns>
        internal IEnumerable<School> GetSchoolsForFaculty(PPMToolContext context, int facultyId, bool activeOnly = true)
        {
            var allSchools = GetAll(context).Where(x => x.Faculty != null && x.Faculty.FacultyId == facultyId);
            if (activeOnly)
            {
                allSchools = allSchools.Where(x => x.IsActive);
            }
            return allSchools;
        }
    }
}
