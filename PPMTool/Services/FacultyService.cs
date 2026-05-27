// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
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
            return context.Faculties
                .Include(x => x.Schools.OrderBy(s => s.Code));
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void Delete(PPMToolContext context, Faculty faculty, bool commitChanges = true)
        {
            context.Remove(faculty);
            if (commitChanges) CommitChanges(context);
        }

        /// <inheritdoc />
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
        /// Detect a duplicate faculty when the name or code matches another one in the DB.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Faculty entity)
        {
            return GetAll(context)
                .Any(x =>
                    x.FacultyId != entity.FacultyId &&
                    (x.Name.Trim().ToLower() == entity.Name.Trim().ToLower() || x.Code.Trim().ToLower() == entity.Code.Trim().ToLower())
                );
        }
    }
}
