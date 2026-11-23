// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class SchoolService : BaseEntityService<School>
    {
        public SchoolService(ILogger<SchoolService> logger) : base(logger)
        {
        }

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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void Delete(PPMToolContext context, School school, bool commitChanges = true)
        {
            context.Remove(school);
            if (commitChanges) CommitChanges(context);
        }

        /// <inheritdoc />
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
        /// Detect a duplicate school: another with the same name in the same faculty, or with the
        /// same code in any faculty. Codes are unique across faculties so that a code alone
        /// identifies a school.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, School entity)
        {
            // Queried in the database, so other schools' faculties needn't be loaded
            var code = entity.Code.Trim().ToLower();
            var name = entity.Name.Trim().ToLower();
            var facultyId = entity.Faculty.FacultyId;
            return context.Schools
                .Any(x =>
                    x.SchoolId != entity.SchoolId &&
                    (x.Code.Trim().ToLower() == code
                        || (x.Name.Trim().ToLower() == name && x.Faculty.FacultyId == facultyId))
                );
        }

        /// <summary>
        /// Get the school with the given code, ignoring case and surrounding whitespace, with its faculty.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <param name="activeOnly"></param>
        /// <returns>The school, or null if there is none.</returns>
        public School GetByCode(PPMToolContext context, string code, bool activeOnly = false)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var normalised = code.Trim().ToLower();
            return context.Schools
                .Include(x => x.Faculty)
                .FirstOrDefault(x => (!activeOnly || x.IsActive) && x.Code.Trim().ToLower() == normalised);
        }

        /// <summary>
        /// Get the school with the given id, with its faculty.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="schoolId"></param>
        /// <param name="activeOnly"></param>
        /// <returns>The school, or null if there is none.</returns>
        public School GetById(PPMToolContext context, int schoolId, bool activeOnly = false)
        {
            return context.Schools
                .Include(x => x.Faculty)
                .FirstOrDefault(x => (!activeOnly || x.IsActive) && x.SchoolId == schoolId);
        }

        /// <summary>
        /// Get the schools for a given faculty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="facultyId"></param>
        /// <param name="activeOnly"></param>
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
