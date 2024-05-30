using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class ProjectService : BaseService<Project>
    {
        /// <summary>
        /// Adds a project. If duplicate found based on name, does not add but returns false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns>-1 if a duplicate name, -2 if duplciate RTP</returns>
        public override int Add(PPMToolContext context, Project projectModel)
        {
            if (DuplicateDetected(context, projectModel))
            {
                return -1;
            }
            if (DuplicateRTPDetected(context, projectModel))
            {
                return -2;
            }

            context.Projects.Add(projectModel);
            context.SaveChanges();
            return projectModel.ProjectId;
        }

        /// <summary>
        /// Duplicate determined by name or RTP number
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Project projectModel)
        {
            return context.Projects.Any(p => p.Name.ToLower().Trim() == projectModel.Name.ToLower().Trim() && projectModel.ProjectId != p.ProjectId);
        }

        private bool DuplicateRTPDetected(PPMToolContext context, Project projectModel)
        {
            return context.Projects.Any(x => x.RTP == projectModel.RTP && projectModel.ProjectId != x.ProjectId);
        }

        /// <summary>
        /// Get project by its ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectID"></param>
        /// <returns></returns>
        internal Project GetById(PPMToolContext context, int? projectID)
        {
            return GetAll(context)
                .FirstOrDefault(p => p.ProjectId == projectID);
        }

        /// <summary>
        /// Update an existing project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns>-1 if a duplicate name, -2 if duplciate RTP</returns>
        public override int Update(PPMToolContext context, Project projectModel)
        {
            if (DuplicateDetected(context, projectModel))
            {
                return -1;
            }
            if (DuplicateRTPDetected(context, projectModel))
            {
                return -2;
            }

            context.Projects.Update(projectModel);
            context.SaveChanges();
            return projectModel.ProjectId;
        }

        /// <summary>
        /// Gets all the projects
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Project> GetAll(PPMToolContext context)
        {
            return context.Projects
                .Include(p => p.SubTasks)
                .ThenInclude(s => s.AssignedResources)
                .ThenInclude(r => r.Person)
                .ThenInclude(pp => pp.Absences)
                .Include(p => p.ProjectManager)
                .Include(p => p.InnateActivity)
                .ToList();
        }

        /// <summary>
        /// Get all the projects but pre-filter to only unfunded ones.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<Project> GetUnfundedProjects(PPMToolContext context)
        {
            return GetAll(context).Where(p => p.ProjectStatus == ProjectStatus.Unfunded);
        }

        /// <summary>
        /// Delete the project from the database.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        public override void Delete(PPMToolContext context, Project projectModel)
        {
            context.Projects.Remove(projectModel);
            context.SaveChanges();
        }

        /// <summary>
        /// Get the project by its RTP number
        /// </summary>
        /// <param name="context"></param>
        /// <param name="RTP"></param>
        /// <returns></returns>
        internal Project GetByRTP(PPMToolContext context, int? RTP)
        {
            return RTP == null ? null : GetAll(context)
                .FirstOrDefault(p => p.RTP == RTP);
        }
    }
}
