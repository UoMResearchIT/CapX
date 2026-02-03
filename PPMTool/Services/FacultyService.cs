using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class FacultyService : BaseEntityService<Faculty>
    {
        /// <summary>
        /// Returns all faculties in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Faculty> GetAll(PPMToolContext context)
        {
            return context.Faculties;
        }

        /// <summary>
        /// Returns faculties in the DB filtered by their IsActive status
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Faculty> GetAllActive(PPMToolContext context)
        {
            return GetAll(context).Where(x => x.IsActive);
        }

        /// <summary>
        /// Updates the faculty in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faculty"></param>
        public override int Update(PPMToolContext context, Faculty faculty, bool commitChanges = true)
        {
            if (DuplicateDetected(context, faculty))
            {
                return -1;
            }
            context.Faculties.Update(faculty);
            if (commitChanges) CommitChanges(context);
            return faculty.FacultyId;
        }

        /// <summary>
        /// Gets the tracking entry for the faculty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faculty"></param>
        /// <returns></returns>
        internal EntityEntry<Faculty> GetEntry(PPMToolContext context, Faculty faculty)
        {
            return context.Entry(faculty);
        }

        /// <summary>
        /// Deletes a faculty from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faculty"></param>
        public override void Delete(PPMToolContext context, Faculty faculty, bool commitChanges = true)
        {
            context.Remove(faculty);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Adds a new faculty to the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faculty"></param>
        public override int Add(PPMToolContext context, Faculty faculty, bool commitChanges = true)
        {
            if (DuplicateDetected(context, faculty))
            {
                return -1;
            }
            context.Add(faculty);
            if (commitChanges) CommitChanges(context);
            return faculty.FacultyId;
        }

        /// <summary>
        /// Detect a duplicate faculty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Faculty entity)
        {
            return GetAll(context).Any(x => (Clean(x.Name) == Clean(entity.Name) || Clean(x.Code) == Clean(entity.Code)) && x.FacultyId != entity.FacultyId);
        }

        /// <summary>
        /// Returns a Faculty entity where the name has been matched
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Faculty GetFacultyByName(PPMToolContext context, string name)
        {
            return GetAll(context).Where(x => (Clean(x.Name) == Clean(name))).First();
        }

        /// <summary>
        /// Returns a Faculty entity where the code has been matched
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Faculty GetFacultyByCode(PPMToolContext context, string code)
        {
            return GetAll(context).Where(x => (Clean(x.Code) == Clean(code))).First();
        }
    }
}
