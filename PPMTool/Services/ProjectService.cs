using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Data;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class ProjectService
    {
        /// <summary>
        /// Adds a project. If duplicate found based on name, does not add but returns false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns></returns>
        internal bool AddProject(PPMToolContext context, Project projectModel)
        {
            if (context.Projects.Any(p => p.Name.ToLower().Trim() == projectModel.Name.ToLower().Trim()))
            {
                // Duplicate found
                return false;
            }

            context.Projects.Add(projectModel);
            context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Get project by its ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectID"></param>
        /// <returns></returns>
        internal Project GetById(PPMToolContext context, int? projectID)
        {
            return context.Projects.FirstOrDefault(p => p.ProjectId == projectID);
        }

        /// <summary>
        /// Gets all the projects
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<Project> GetAll(PPMToolContext context)
        {
            return context.Projects.ToList();
        }
    }
}
