using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.API.Helpers
{
    /// <summary>
    /// Class to hold all helper methods for timesheets endpoints.
    /// </summary>
    internal class TimesheetsHelpers
    {
        /// <summary>
        /// Apply optional date range filtering to a timesheet query.
        /// Handles weekly timesheets that may overlap with the date range boundaries.
        /// </summary>
        /// <param name="query">The base query to filter</param>
        /// <param name="start">Optional start date (inclusive)</param>
        /// <param name="endExclusive">Optional end date (exclusive)</param>
        /// <returns>The filtered query</returns>
        internal static IQueryable<Timesheet> ApplyDateRangeFilter(
            IQueryable<Timesheet> query, DateTime? start, DateTime? endExclusive)
        {
            if (start.HasValue && endExclusive.HasValue)
            {
                var startValue = start.Value;
                var endValue = endExclusive.Value;
                // Timesheet overlaps if it starts before the end AND ends after the start
                return query.Where(t => t.StartDate < endValue && t.StartDate.AddDays(7) > startValue);
            }
            else if (start.HasValue)
            {
                var startValue = start.Value;
                return query.Where(t => t.StartDate >= startValue);
            }
            else if (endExclusive.HasValue)
            {
                var endValue = endExclusive.Value;
                return query.Where(t => t.StartDate < endValue);
            }

            return query;
        }

        /// <summary>
        /// Build a timesheet query with option to only look at approved.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="approvedOnly">Whether the method should filter to just the approved timesheets</param>
        internal static IQueryable<Timesheet> BuildTimesheetQuery(
            PPMToolContext context, bool approvedOnly = false)
        {
            IQueryable<Timesheet> query = context.Timesheets
                .Include(t => t.Owner)
                    .ThenInclude(o => o.LineManager)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk!.InnateCode);

            // Add filter to query based on status if necessary
            if (approvedOnly)
            {
                query = query.Where(t => t.Status == Enums.TimesheetStatus.Approved);
            }

            return query;
        }

        /// <summary>
        /// Returns an in-memory collection of timesheets that match the specified code and optional task name.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="code"></param>
        /// <param name="taskName"></param>
        /// <returns></returns>
        internal static IEnumerable<Timesheet> GetAllMatchingCodeAndTask(IQueryable<Timesheet> query, string code, string? taskName)
        {
            // Add code and task filter to query
            var normalisedCode = code.Trim().ToLowerInvariant();
            var normalisedTask = taskName?.Trim().ToLowerInvariant();

            return query
                .Where(x => x.TimesheetEntries.Any(e =>
                    e.InnateCodeTask != null &&
                    e.InnateCodeTask.InnateCode != null)
                )

                // Have to pull into memory to do the string comparisons with trimming and normalisation
                .AsEnumerable()
                .Where(t =>
                    t.TimesheetEntries.Any(e =>
                        e.InnateCodeTask.InnateCode.ActivityCode.Trim().ToLowerInvariant() == normalisedCode &&
                        (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName.Trim().ToLowerInvariant() == normalisedTask)
                    )
                );
        }

        /// <summary>
        /// Method to map timesheet DTOs to CSV row data in a consistent way for all timesheets endpoints.
        /// </summary>
        /// <param name="timesheetsAsDTOs"></param>
        /// <returns></returns>
        internal static IEnumerable<TimesheetCSVRowData> MapToCsvRowData(IEnumerable<TimesheetsDTO> timesheetsAsDTOs)
        {
            return timesheetsAsDTOs.SelectMany(ts =>
                ts.Entries.Select(e =>
                    new TimesheetCSVRowData(
                        ts.OwnerName,
                        ts.StartDate.ToString("yyyy-MM-dd"),
                        ts.Status,
                        ts.Info ?? "",
                        e.InnateCode,
                        e.InnateCodeName,
                        e.TaskName,
                        e.Duty,
                        e.MondayHours,
                        e.TuesdayHours,
                        e.WednesdayHours,
                        e.ThursdayHours,
                        e.FridayHours,
                        e.SaturdayHours,
                        e.SundayHours,
                        e.MondayHours + e.TuesdayHours + e.WednesdayHours +
                            e.ThursdayHours + e.FridayHours + e.SaturdayHours + e.SundayHours
                    )
                )
            );
        }

        /// <summary>
        /// Map timesheet entities to DTOs. Reuses the same mapping logic across all timesheet endpoints.
        /// </summary>
        /// <param name="timesheets"></param>
        internal static List<TimesheetsDTO> MapToTimesheetDTOs(List<Timesheet> timesheets)
        {
            return timesheets.Select(t => new TimesheetsDTO(
                TimesheetId: t.TimesheetId,
                OwnerId: t.OwnerId,
                OwnerName: t.Owner?.Name ?? "Unknown",
                CreatedDate: t.CreatedDate,
                StartDate: t.StartDate,
                Status: t.Status.GetDescription(),
                DateStatusChanged: t.DateStatusChanged,
                Info: t.Info,
                Entries: t.TimesheetEntries.Select(e => new TimesheetEntryDTO(
                    TimesheetEntryId: e.TimesheetEntryId,
                    InnateCodeTaskId: e.InnateCodeTask?.InnateCodeTaskId ?? 0,
                    InnateCode: e.InnateCodeTask?.InnateCode?.ActivityCode ?? string.Empty,
                    InnateCodeName: e.InnateCodeTask?.InnateCode?.ActivityName ?? string.Empty,
                    TaskName: e.InnateCodeTask?.TaskName ?? string.Empty,
                    Duty: e.InnateCodeTask?.Duty.GetDescription() ?? "None",
                    MondayHours: e.MondayHours,
                    TuesdayHours: e.TuesdayHours,
                    WednesdayHours: e.WednesdayHours,
                    ThursdayHours: e.ThursdayHours,
                    FridayHours: e.FridayHours,
                    SaturdayHours: e.SaturdayHours,
                    SundayHours: e.SundayHours
                )).ToList()
            )).ToList();
        }

        /// <summary>
        /// Calculate aggregated hours summary by person across all timesheets.
        /// </summary>
        /// <param name="timesheets"></param>
        internal static List<PersonHoursSummaryDTO> CalculatePersonHoursSummary(List<TimesheetsDTO> timesheets)
        {
            return timesheets
                .GroupBy(ts => ts.OwnerName)
                .Select(g => new PersonHoursSummaryDTO(
                    PersonName: g.Key,
                    TotalHours: g.SelectMany(ts => ts.Entries)
                        .Sum(e => e.MondayHours + e.TuesdayHours + e.WednesdayHours +
                                  e.ThursdayHours + e.FridayHours + e.SaturdayHours + e.SundayHours)
                ))
                .OrderByDescending(s => s.TotalHours)
                .ToList();
        }

        /// <summary>
        /// Gets the InnateCode entity matching the specified activity code.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activityCode"></param>
        /// <returns>The matching InnateCode entity, or null if not found.</returns>
        internal static InnateCode? GetInnateCode(PPMToolContext context, string activityCode)
        {
            var normalisedCode = activityCode.Trim().ToLowerInvariant();
            return context.InnateCodes
                .AsEnumerable() // Map everything into memory as we need to convert to lower
                .FirstOrDefault(ic => ic.ActivityCode.Trim().ToLowerInvariant() == normalisedCode);
        }

        /// <summary>
        /// Maps timesheets to grouped bookings format: grouped by task, then by person, with weekly hours breakdown.
        /// </summary>
        /// <param name="timesheets">List of timesheet entities</param>
        /// <param name="projectCode">The project/activity code for the response</param>
        /// <param name="code">The activity code to filter entries by</param>
        /// <param name="taskName">Optional task name to filter entries by</param>
        /// <returns>Grouped timesheet bookings response</returns>
        internal static TimesheetBookingsByCodeTaskResponseDTO MapToGroupedBookingsDTO(
            List<Timesheet> timesheets,
            string projectCode,
            string code,
            string? taskName)
        {
            // Normalize filter values for comparison
            var normalisedCode = code.Trim().ToLowerInvariant();
            var normalisedTask = taskName?.Trim().ToLowerInvariant();
            var dateFormat = "dd/MM/yyyy";

            // Group all entries by task name and person, aggregating hours by week
            // Filter entries to only include those matching the requested code and optional task
            var taskGroups = timesheets
                .SelectMany(ts => ts.TimesheetEntries
                    .Where(e =>
                        e.InnateCodeTask != null &&
                        e.InnateCodeTask.InnateCode != null &&
                        e.InnateCodeTask.InnateCode.ActivityCode.Trim().ToLowerInvariant() == normalisedCode &&
                        (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName.Trim().ToLowerInvariant() == normalisedTask))
                    .Select(e => new
                    {
                        TaskName = e.InnateCodeTask.TaskName,
                        PersonName = ts.Owner?.Name ?? "Unknown",
                        WeekStart = ts.StartDate,
                        Entry = e
                    })
                )
                .GroupBy(x => x.TaskName)
                .OrderBy(g => g.Key) // Alphabetical order for tasks
                .Select(taskGroup =>
                {
                    // Group by person within each task
                    var personAllocations = taskGroup
                        .GroupBy(x => x.PersonName)
                        .OrderBy(g => g.Key) // Alphabetical order for persons
                        .Select(personGroup =>
                        {
                            // Aggregate hours by week for this person
                            var weeklyHoursDict = new Dictionary<DateTime, double>();

                            foreach (var item in personGroup)
                            {
                                var weekStart = item.WeekStart;
                                var entry = item.Entry;

                                // Add hours for each day of the week
                                var days = new[]
                                {
                                    (offset: 0, hours: entry.MondayHours),
                                    (offset: 1, hours: entry.TuesdayHours),
                                    (offset: 2, hours: entry.WednesdayHours),
                                    (offset: 3, hours: entry.ThursdayHours),
                                    (offset: 4, hours: entry.FridayHours),
                                    (offset: 5, hours: entry.SaturdayHours),
                                    (offset: 6, hours: entry.SundayHours)
                                };

                                foreach (var (offset, hours) in days)
                                {
                                    var date = weekStart.AddDays(offset);

                                    if (weeklyHoursDict.ContainsKey(date))
                                    {
                                        weeklyHoursDict[date] += hours;
                                    }
                                    else
                                    {
                                        weeklyHoursDict[date] = hours;
                                    }
                                }
                            }

                            // Convert to formatted string dictionary and sort chronologically
                            var weeklyHours = weeklyHoursDict
                                .OrderBy(kvp => kvp.Key) // Chronological order
                                .ToDictionary(
                                    kvp => kvp.Key.ToString(dateFormat),
                                    kvp => kvp.Value
                                );

                            var total = weeklyHours.Values.Sum();

                            return new PersonAllocationDTO(
                                Person: personGroup.Key,
                                BookedHours: weeklyHours,
                                Total: total
                            );
                        })
                        .ToList();

                    var taskTotal = personAllocations.Sum(p => p.Total);

                    return new TaskAllocationDTO(
                        TaskName: taskGroup.Key,
                        Allocations: personAllocations,
                        TaskTotal: taskTotal
                    );
                })
                .ToList();

            return new TimesheetBookingsByCodeTaskResponseDTO(
                ProjectCode: projectCode,
                Tasks: taskGroups
            );
        }

        /// <summary>
        /// Maps grouped bookings DTO to CSV row data, flattening the hierarchical structure.
        /// Each row represents one date entry for a person working on a task, with summary rows.
        /// </summary>
        /// <param name="groupedData">The grouped timesheet bookings</param>
        /// <returns>Flattened CSV rows with person and task totals</returns>
        internal static IEnumerable<GroupedTimesheetCSVRowData> MapToGroupedCsvRowData(TimesheetBookingsByCodeTaskResponseDTO groupedData)
        {
            foreach (var task in groupedData.Tasks)
            {
                foreach (var person in task.Allocations)
                {
                    // Output all the person's booked hours
                    foreach (var entry in person.BookedHours)
                    {
                        yield return new GroupedTimesheetCSVRowData(
                            ProjectCode: groupedData.ProjectCode,
                            TaskName: task.TaskName,
                            PersonName: person.Person,
                            Date: entry.Key,
                            Hours: entry.Value
                        );
                    }

                    // Add person total summary row
                    yield return new GroupedTimesheetCSVRowData(
                        ProjectCode: "",
                        TaskName: "",
                        PersonName: $"Person total: {person.Total}",
                        Date: "",
                        Hours: 0
                    );
                }

                // Add task total summary row after all people in the task
                yield return new GroupedTimesheetCSVRowData(
                    ProjectCode: "",
                    TaskName: $"Task total: {task.TaskTotal}",
                    PersonName: "",
                    Date: "",
                    Hours: 0
                );
            }
        }
    }
}
