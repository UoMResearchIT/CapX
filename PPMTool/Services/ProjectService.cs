using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    public class ProjectService : BaseEntityService<Project>
    {
        private readonly SettingsService settingsService;

        public ProjectService(SettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        /// <inheritdoc />
        public override int Add(PPMToolContext context, Project projectModel, bool commitChanges = true)
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
            if (commitChanges) CommitChanges(context);
            return projectModel.ProjectId;
        }

        /// <summary>
        /// Duplicate determined by name
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Project projectModel)
        {
            return context.Projects.Any(p => p.Name.Trim().ToLower() == projectModel.Name.Trim().ToLower() && projectModel.ProjectId != p.ProjectId);
        }

        /// <summary>
        /// Duplicate determined by RTP
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns></returns>
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
        /// <param name="commitChanges"></param>
        /// <returns>-1 if a duplicate name, -2 if duplicate abbreviation</returns>
        public override int Update(PPMToolContext context, Project projectModel, bool commitChanges = true)
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
            if (commitChanges) CommitChanges(context);
            return projectModel.ProjectId;
        }

        /// <summary>
        /// Gets all the projects with all their related tables -- pretty heavy operation now! Needs some optimisation!
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Project> GetAll(PPMToolContext context)
        {
            return context.Projects
                .Include(p => p.School)
                    .ThenInclude(s => s.Faculty)
                .Include(p => p.SubTasks)
                    .ThenInclude(s => s.AssignedResources)
                        .ThenInclude(r => r.Person)
                            .ThenInclude(r => r.WorkloadModelChanges)
                .Include(p => p.SubTasks)
                    .ThenInclude(x => x.SkillsRequired)
                .Include(x => x.SubTasks)
                    .ThenInclude(s => s.AssignedResources)
                        .ThenInclude(x => x.FundedFrom)
                .Include(x => x.SubTasks)
                    .ThenInclude(s => s.AssignedResources)
                        .ThenInclude(r => r.Person)
                            .ThenInclude(pp => pp.Absences)
                .Include(p => p.ProjectManager)
                .Include(p => p.InnateActivity)
                .Include(p => p.Followers)
                .Include(p => p.FundingSources)
                .ToList();
        }

        /// <summary>
        /// Gets project table entities without any includes
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Project> GetAllShallow(PPMToolContext context)
        {
            return context.Projects;
        }

        /// <summary>
        /// Get all the projects but pre-filter to only unfunded ones.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<Project> GetUnfundedProjects(PPMToolContext context)
        {
            return GetAll(context).Where(p => p.ProjectStatus.IsUnfunded());
        }

        /// <inheritdoc />
        public override void Delete(PPMToolContext context, Project projectModel, bool commitChanges = true)
        {
            context.Projects.Remove(projectModel);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Get the project by its RTP number
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rtp"></param>
        /// <returns></returns>
        internal Project GetByRTP(PPMToolContext context, int? rtp)
        {
            return rtp == null ? null : GetAll(context)
                .FirstOrDefault(p => p.RTP == rtp);
        }

        /// <summary>
        /// Use the entry tracking to determine the old status of a project model before writing changes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectModel"></param>
        /// <returns></returns>
        internal ProjectStatus GetOldStatus(PPMToolContext context, Project projectModel)
        {
            var success = context.Entry(projectModel).OriginalValues.TryGetValue<ProjectStatus>(nameof(Project.ProjectStatus), out var status);
            if (!success) throw new InvalidOperationException("Could not determine the old status of the project model.");
            return status;
        }

        /// <summary>
        /// Returns the project manager as a person entity for the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        internal Person GetProjectManager(PPMToolContext context, int projectId)
        {
            return context.Projects
                .Include(x => x.ProjectManager)
                .FirstOrDefault(x => x.ProjectId == projectId)?
                .ProjectManager;
        }

        /// <summary>
        /// Returns the project name prefixed by the abbreviation from settings
        /// </summary>
        public string GetFullName(Project project)
        {
            var abbreviation = settingsService.GetSetting(SettingType.ProjectAbbreviation);
            abbreviation ??= string.Empty;
            return $"{abbreviation}-{project?.RTP} {project?.Name}";
        }
    }
}
