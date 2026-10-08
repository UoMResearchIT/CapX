// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a project suitable for API data exports.
    /// </summary>
    /// <param name="ProjectId">The project RTP ID used by assignment exports.</param>
    /// <param name="CapXProjectId">The internal CapX project entity ID.</param>
    /// <param name="Name">The name of the project.</param>
    /// <param name="PrincipalInvestigator">The principal investigator on the project.</param>
    /// <param name="ProjectManagerId">The person ID of the project manager, if one is assigned.</param>
    /// <param name="ProjectManagerName">The name of the project manager, if one is assigned.</param>
    /// <param name="TimesheetActivityCode">The timesheet activity code linked to the project, if one is assigned.</param>
    /// <param name="TimesheetActivityName">The timesheet activity name linked to the project, if one is assigned.</param>
    /// <param name="RequestDocLink">The request document link.</param>
    /// <param name="ScrumProjectLink">The scrum project link.</param>
    /// <param name="ProjectStatus">The current project status.</param>
    /// <param name="SchoolCode">The code of the School the project sits in, as accepted by PUT /api/projects/update.</param>
    /// <param name="Budget">The amount the PI has requested from the funder.</param>
    /// <param name="CostModel">The cost model's enum name (e.g. "TechOnly"), as accepted by PUT /api/projects/update.</param>
    /// <param name="DayRate">The day rate; zero unless CostModel is DayRate.</param>
    /// <param name="Description">The project description, as HTML.</param>
    public sealed record ProjectDTO(
        int ProjectId,
        int CapXProjectId,
        string Name,
        string PrincipalInvestigator,
        int? ProjectManagerId,
        string ProjectManagerName,
        string TimesheetActivityCode,
        string TimesheetActivityName,
        string RequestDocLink,
        string ScrumProjectLink,
        string ProjectStatus,
        string SchoolCode,
        double Budget,
        string CostModel,
        double DayRate,
        string Description
    );
}
