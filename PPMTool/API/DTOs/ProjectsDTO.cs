// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a project suitable for API data exports.
    /// </summary>
    /// <param name="ProjectId">The project ID of the entity in the database.</param>
    /// <param name="CapXProjectId">The UI-visible project reference number.</param>
    /// <param name="Name">The name of the project.</param>
    /// <param name="PrincipalInvestigator">The principal investigator on the project.</param>
    /// <param name="ProjectManagerId">The person ID of the project manager, if one is assigned.</param>
    /// <param name="ProjectManagerName">The name of the project manager, if one is assigned.</param>
    /// <param name="TimesheetActivityCode">The timesheet activity code linked to the project, if one is assigned.</param>
    /// <param name="TimesheetActivityName">The timesheet activity name linked to the project, if one is assigned.</param>
    /// <param name="RequestDocLink">The request document link.</param>
    /// <param name="ScrumProjectLink">The scrum project link.</param>
    /// <param name="ProjectStatus">The current project status.</param>
    /// <param name="SchoolId">The ID of the school associated with the project.</param>
    /// <param name="CostModel">The cost model for the project.</param>
    /// <param name="StartDate">The start date of the project.</param>
    /// <param name="EndDate">The end date of the project.</param>
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
        int SchoolId,
        string CostModel,
        DateOnly StartDate,
        DateOnly EndDate
    );
}
