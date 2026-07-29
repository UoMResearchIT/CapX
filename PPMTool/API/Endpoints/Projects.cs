// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Project endpoint methods.
/// </summary>
public static class Projects
{
    /// <summary>
    /// Get all projects.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProjectDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAllProjectsAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http)
    {
        try
        {
            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetAllProjects: Caller does not have permission to access project data.");
                return Results.Unauthorized();
            }

            var projects = await context.Projects
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .OrderBy(x => x.RTP)
                .ToListAsync();

            var projectDtos = projects.Select(x => new ProjectDTO(
                ProjectId: x.RTP,
                CapXProjectId: x.ProjectId,
                Name: x.Name,
                PrincipalInvestigator: x.PI,
                ProjectManagerId: x.ProjectManager?.PersonId,
                ProjectManagerName: x.ProjectManager?.Name,
                TimesheetActivityCode: x.InnateActivity?.ActivityCode,
                TimesheetActivityName: x.InnateActivity?.ActivityName,
                RequestDocLink: x.RequestDocLink,
                ScrumProjectLink: x.ScrumProjectLink,
                ProjectStatus: x.ProjectStatus.GetDescription()
            )).ToList();

            logger.LogInformation("API: GetAllProjects: Returned {Count} project records.", projectDtos.Count);
            return Results.Json(projectDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllProjects: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one project by RTP project ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="projectId">The RTP project ID.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetProjectByIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? projectId)
    {
        try
        {
            if (!projectId.HasValue || projectId.Value <= 0)
            {
                logger.LogWarning("API: GetProjectById: Invalid projectId {ProjectId}", projectId);
                return Results.BadRequest("Project ID must be greater than zero.");
            }

            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetProjectById: Caller does not have permission to access project data.");
                return Results.Unauthorized();
            }

            var project = await context.Projects
                .Include(x => x.ProjectManager)
                .Include(x => x.InnateActivity)
                .FirstOrDefaultAsync(x => x.RTP == projectId);

            if (project == null)
            {
                logger.LogWarning("API: GetProjectById: No project found for projectId {ProjectId}", projectId);
                return Results.NotFound();
            }

            var dto = new ProjectDTO(
                ProjectId: project.RTP,
                CapXProjectId: project.ProjectId,
                Name: project.Name,
                PrincipalInvestigator: project.PI,
                ProjectManagerId: project.ProjectManager?.PersonId,
                ProjectManagerName: project.ProjectManager?.Name,
                TimesheetActivityCode: project.InnateActivity?.ActivityCode,
                TimesheetActivityName: project.InnateActivity?.ActivityName,
                RequestDocLink: project.RequestDocLink,
                ScrumProjectLink: project.ScrumProjectLink,
                ProjectStatus: project.ProjectStatus.GetDescription()
            );

            logger.LogInformation("API: GetProjectById: Returned project record for projectId {ProjectId}", projectId);
            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetProjectById: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
