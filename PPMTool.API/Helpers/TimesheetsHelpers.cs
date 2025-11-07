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
        /// Build a timesheet query filtered by code and optional task name.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <param name="taskName"></param>
        /// <param name="approvedOnly">Whether the method should filter to just the approved timesheets</param>
        internal static IQueryable<Timesheet> BuildTimesheetQueryWithCodeAndTaskFilter(
            PPMToolContext context, string code, string? taskName, bool approvedOnly = false)
        {
            IQueryable<Timesheet> query = context.Timesheets
                .Include(t => t.Owner)
                .Include(t => t.TimesheetEntries)
                    .ThenInclude(e => e.InnateCodeTask)
                        .ThenInclude(tk => tk!.InnateCode);

            // Add filter to query based on status if necessary
            if (approvedOnly)
            {
                query = query.Where(t => t.Status == Enums.TimesheetStatus.Approved);
            }

            // Add code and task filter to query
            query = query
                .Where(t =>
                    t.TimesheetEntries.Any(e =>
                        e.InnateCodeTask != null &&
                        e.InnateCodeTask.InnateCode != null &&
                        e.InnateCodeTask.InnateCode.ActivityCode.Trim().ToLowerInvariant() == code.Trim().ToLowerInvariant() &&
                        (string.IsNullOrWhiteSpace(taskName) || e.InnateCodeTask.TaskName.Trim().ToLowerInvariant() == taskName.Trim().ToLowerInvariant())
                    ));

            return query;
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
        /// Checks whether a timesheet code exists with the specified activity code.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activityCode"></param>
        /// <returns></returns>
        internal static bool IsValidTimesheetCode(PPMToolContext context, string activityCode)
        {
            return context.InnateCodes.Any(ic => ic.ActivityCode.Trim().ToLowerInvariant() == activityCode.Trim().ToLowerInvariant());
        }
    }
}
