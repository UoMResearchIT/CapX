// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;

namespace PPMTool.API.Helpers
{
    /// <summary>
    /// Class to hold all helper methods for assignments endpoints.
    /// </summary>
    internal class AssignmentsHelper
    {
        /// <summary>
        /// Defines which project statuses should be excluded from queries.
        /// </summary>
        private static readonly ProjectStatus[] cancelledStatuses = new[]
        {
            ProjectStatus.CancelledByCustomer,
            ProjectStatus.CancelledBidFailed,
            ProjectStatus.CancelledNoResource
        };

        /// <summary>
        /// Uses the methods used by the finance export to construct assignments for all people in the system in the date range.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static async Task<IList<AssignmentDTO>> GetAssignmentChunksAsync(PPMToolContext context, DateTime? start, DateTime? end)
        {
            // Validate the date range
            if (start is null)
            {
                start = DateTime.MinValue;
            }
            if (end is null)
            {
                end = DateTime.MaxValue;
            }
            var startValue = start.Value;
            var endValue = end.Value;

            // Get the projects and financial references from the database
            // Use explicit expressions here so they are LINQ-SQL translatable
            var projectsInWindow = await context.Projects
                .Include(x => x.SubTasks)
                    .ThenInclude(x => x.AssignedResources)
                        .ThenInclude(x => x.Person)
                .Where(x => !cancelledStatuses.Contains(x.ProjectStatus) &&
                            x.StartDate.Date <= endValue.Date &&
                            x.EndDate.Date >= startValue.Date)
                .ToListAsync();
            Debug.WriteLine($"** {projectsInWindow.Count} projects running during the window.");

            // Create blank list of data
            var assignmentChunks = new List<AssignmentChunk>();

            // Get data for each person active in the window
            var peopleActive = await context.People
                .Include(x => x.WorkloadModelChanges)
                .Where(x => x.StartDate <= endValue && (x.EndDate == null || x.EndDate >= startValue))
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Build tasks for each person
            foreach (var person in peopleActive)
            {
                // Filter list of tasks to just those assigned to this person
                // Using LINQ-to-objects on already-loaded projects/tasks
                var tasksInWindow = projectsInWindow
                    .SelectMany(x => x.SubTasks)
                    .Where(x => x.AssignedResources.Any(ar => ar.Person.PersonId == person.PersonId) &&
                                x.StartDate.Date <= endValue.Date &&
                                x.EndDate.Date >= startValue.Date)
                    .ToList();
                Debug.WriteLine($"** {tasksInWindow.Count} tasks within window for {person.Name}");

                // Represent the assignments in the window as chunks.
                var data = AssignmentHelper.GetAssignmentChunks(
                    person,
                    projectsInWindow,
                    finrefs: null,
                    startValue,
                    endValue,
                    tasksInWindow
                );

                Debug.WriteLine($"** Built {data.Count()} rows for {person.Name}");
                assignmentChunks.AddRange(data);
            }
            assignmentChunks.Sort((x, y) => x.EmployeeName.CompareTo(y.EmployeeName));
            Debug.WriteLine($"** {assignmentChunks.Count()} assignment entries generated!");

            // Map the result to DTOs
            var assignmentDTOs = assignmentChunks.Select(chunk =>
                new AssignmentDTO(

                    // Map properties from chunk to AssignmentDTO
                    ProjectId: chunk.ProjectId,
                    ProjectName: chunk.ProjectName,
                    PersonName: chunk.EmployeeName,
                    Grade: chunk.Grade,
                    FTE: chunk.FTE,
                    TaskName: chunk.TaskName,
                    StartDate: chunk.StartDate,
                    EndDate: chunk.EndDate,
                    LeadershipTask: chunk.IsLeadershipAssignment
                )
            ).ToList();

            // Return the DTOs
            return assignmentDTOs;
        }
    }
}
