// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.API.Helpers
{
    /// <summary>
    /// Reusable helpers for API import workflows.
    /// </summary>
    public static class ImportHelper
    {
        /// <summary>
        /// Removes the <see cref="DateTime.Kind"/> without changing the clock value.
        /// </summary>
        /// <param name="dt">The date/time to normalize.</param>
        /// <returns>The same date/time value with <see cref="DateTimeKind.Unspecified"/> kind.</returns>
        internal static DateTime AsUnspecifiedKind(DateTime dt) =>
            DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

        /// <summary>
        /// Parses an enum value and rejects undefined values.
        /// </summary>
        /// <typeparam name="TEnum">Enum type.</typeparam>
        /// <param name="value">Value to parse.</param>
        /// <param name="result">Parsed enum value when successful.</param>
        /// <returns><c>true</c> when parsing succeeds to a defined enum value; otherwise <c>false</c>.</returns>
        internal static bool TryParseDefined<TEnum>(string value, out TEnum result) where TEnum : struct, Enum =>
            Enum.TryParse(value, out result) && Enum.IsDefined(result);

        /// <summary>
        /// Parses a cost model given either as its enum name or as its description, the form
        /// project reads return, so a value read can be written back.
        /// </summary>
        /// <param name="value">Value to parse.</param>
        /// <param name="result">Parsed cost model when successful.</param>
        /// <returns><c>true</c> when the value names a cost model; otherwise <c>false</c>.</returns>
        internal static bool TryParseCostModel(string value, out CostModel result)
        {
            if (TryParseDefined(value, out result)) return true;
            foreach (var model in Enum.GetValues<CostModel>())
            {
                if (string.Equals(model.GetDescription(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result = model;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether a number contains significant digits beyond three decimal places.
        /// </summary>
        /// <param name="number">Number to test.</param>
        /// <returns><c>true</c> when precision exceeds three decimal places; otherwise <c>false</c>.</returns>
        internal static bool HasDigitsAfterThirdDecimalPlace(double number)
        {
            var truncated = Math.Truncate(number * 1000) / 1000;
            return number != truncated;
        }

        /// <summary>
        /// Returns default tasks for newly created innate codes tied to imported projects.
        /// </summary>
        /// <returns>Default innate-code task templates.</returns>
        internal static List<InnateCodeTask> DefaultInnateCodeTasks() => new()
        {
            new() { TaskName = "Development", Duty = Duty.ProjectWork },
            new() { TaskName = "Management", Duty = Duty.ProjectAndServiceMgmt },
            new() { TaskName = "Maintenance", Duty = Duty.ProjectWork }
        };

        /// <summary>
        /// Enumerates day-hour values for create-or-update timesheet validation.
        /// </summary>
        /// <param name="request">Incoming timesheet-entry request.</param>
        /// <returns>Named day-hour pairs.</returns>
        internal static IEnumerable<(string Label, double Hours)> DayHours(ImportTimesheetEntryDTO request) =>
            DayHoursCore(
                request.MondayHours,
                request.TuesdayHours,
                request.WednesdayHours,
                request.ThursdayHours,
                request.FridayHours,
                request.SaturdayHours,
                request.SundayHours);

        /// <summary>
        /// Enumerates nullable day-hour values for timesheet update validation.
        /// </summary>
        /// <param name="request">Incoming timesheet-entry update request.</param>
        /// <returns>Named day-hour pairs.</returns>
        internal static IEnumerable<(string Label, double? Hours)> DayHours(UpdateTimesheetEntryRequestDTO request) =>
            DayHoursCore(
                request.MondayHours,
                request.TuesdayHours,
                request.WednesdayHours,
                request.ThursdayHours,
                request.FridayHours,
                request.SaturdayHours,
                request.SundayHours);

        /// <summary>
        /// Enumerates labeled day-hour values using a shared implementation for import DTO variants.
        /// </summary>
        /// <typeparam name="THours">Hour value type.</typeparam>
        /// <param name="mondayHours">Monday hours.</param>
        /// <param name="tuesdayHours">Tuesday hours.</param>
        /// <param name="wednesdayHours">Wednesday hours.</param>
        /// <param name="thursdayHours">Thursday hours.</param>
        /// <param name="fridayHours">Friday hours.</param>
        /// <param name="saturdayHours">Saturday hours.</param>
        /// <param name="sundayHours">Sunday hours.</param>
        /// <returns>Named day-hour pairs.</returns>
        private static IEnumerable<(string Label, THours Hours)> DayHoursCore<THours>(
            THours mondayHours,
            THours tuesdayHours,
            THours wednesdayHours,
            THours thursdayHours,
            THours fridayHours,
            THours saturdayHours,
            THours sundayHours)
        {
            yield return (nameof(ImportTimesheetEntryDTO.MondayHours), mondayHours);
            yield return (nameof(ImportTimesheetEntryDTO.TuesdayHours), tuesdayHours);
            yield return (nameof(ImportTimesheetEntryDTO.WednesdayHours), wednesdayHours);
            yield return (nameof(ImportTimesheetEntryDTO.ThursdayHours), thursdayHours);
            yield return (nameof(ImportTimesheetEntryDTO.FridayHours), fridayHours);
            yield return (nameof(ImportTimesheetEntryDTO.SaturdayHours), saturdayHours);
            yield return (nameof(ImportTimesheetEntryDTO.SundayHours), sundayHours);
        }
    }
}
