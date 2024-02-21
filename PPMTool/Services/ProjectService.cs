using System;
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
        /// <returns></returns>
        public override int Add(PPMToolContext context, Project projectModel)
        {
            if (context.Projects.Any(p => p.Name.ToLower().Trim() == projectModel.Name.ToLower().Trim()) || context.Projects.Any(x => x.RTP == projectModel.RTP))
            {
                // Duplicate found
                return -1;
            }

            context.Projects.Add(projectModel);
            context.SaveChanges();
            return projectModel.ProjectId;
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
        public override void Update(PPMToolContext context, Project projectModel)
        {
            context.Projects.Update(projectModel);
            context.SaveChanges();
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
                .Include(p => p.ProjectManager)
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
