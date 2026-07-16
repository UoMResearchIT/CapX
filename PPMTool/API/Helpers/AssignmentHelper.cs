// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
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
        /// Uses the methods used by the finance export to construct assignments for all people in the system in the date range.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static async Task<IList<AssignmentDTO>> GetAssignmentChunksAsync(PPMToolContext context, DateTime? start, DateTime? end)
        {
            // Get the projects and financial references from the database
            var projectsInWindow = context.Projects
                .Where(x => !x.ProjectStatus.IsCancelled() && x.IsWithin(start, end));
            Debug.WriteLine($"** {projectsInWindow.Count()} projects running during the window.");

            // Create blank list of data
            var assignmentChunks = new List<AssignmentChunk>();

            // Get data for each person active in the window
            var peopleActive = context.People
                .Where(x => x.StartDate <= end && (x.EndDate == null || x.EndDate >= start))
                .OrderBy(x => x.Name)
                .ToList();

            // Build tasks for each person
            foreach (var person in peopleActive)
            {
                // Filter list of tasks to just those assigned to this person
                var tasksInWindow = projectsInWindow
                    .SelectMany(x => x.SubTasks)
                    .Where(x => x.AssignedResources
                        .Any(x => x.Person.PersonId == person.PersonId) &&
                        x.IsWithin(start, end)
                    );
                Debug.WriteLine($"** {tasksInWindow.Count()} tasks within window for {person.Name}");

                // Represent the assignments in the window as chunks.
                var data = AssignmentHelper.GetAssignmentChunks(
                    person,
                    projectsInWindow,
                    finrefs: null,
                    start,
                    end,
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
            return new List<AssignmentDTO>();
        }
    }
}
