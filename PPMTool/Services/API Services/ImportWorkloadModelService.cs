// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for API import operations that validate and mutate <see cref="WorkloadModelChange"/> entities.
    /// </summary>
    public class ImportWorkloadModelService
    {
        /// <summary>
        /// Validates an import workload-model-change request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming workload-model-change request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateWorkloadModelChange(PPMToolContext context, ImportWorkloadModelChangeDTO request)
        {
            var errors = new List<string>();

            if (ImportHelper.FindUserByUsername(context, request.Username)?.Person == null)
                errors.Add($"Username '{request.Username}' not found, or has no linked Person");

            if (request.ChangeDate.TimeOfDay != TimeSpan.Zero)
                errors.Add($"ChangeDate '{request.ChangeDate:O}' must be a date with no time of day");

            if (request.Grade < 4 || request.Grade > 9)
                errors.Add($"Grade {request.Grade} is out of range (must be 4-9)");

            foreach (var (label, fte) in DutyFTEs(request))
            {
                if (fte < 0.0 || fte > 1.0) errors.Add($"{label} {fte} is out of range (must be 0.0-1.0)");
            }

            return errors;
        }

        /// <summary>
        /// Creates or updates one workload model change for a person at a specific change date.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming workload-model-change request.</param>
        /// <returns>Identifier payload indicating whether a record was created.</returns>
        public ImportWorkloadModelChangeResponseDTO CreateOrUpdateWorkloadModelChange(PPMToolContext context, ImportWorkloadModelChangeDTO request)
        {
            var person = ImportHelper.FindUserByUsername(context, request.Username)!.Person!;

            var changeDate = ImportHelper.AsUnspecifiedKind(request.ChangeDate);
            var change = person.WorkloadModelChanges.FirstOrDefault(c => c.ChangeDate == changeDate);
            var created = change == null;
            if (change == null)
            {
                change = new WorkloadModelChange { Person = person };
                context.WorkloadModelChanges.Add(change);
            }

            change.ChangeDate = changeDate;
            change.Grade = request.Grade;
            change.ProjectWorkFTE = request.ProjectWorkFTE;
            change.BusinessAsUsualFTE = request.BusinessAsUsualFTE;
            change.PersonalDevelopmentFTE = request.PersonalDevelopmentFTE;
            change.StaffManagementFTE = request.StaffManagementFTE;
            change.ArchitectureFTE = request.ArchitectureFTE;
            change.ServiceManagementFTE = request.ServiceManagementFTE;
            change.ProjectManagementFTE = request.ProjectManagementFTE;
            change.Notes = request.Notes;
            context.SaveChangesWithRetry();

            return new ImportWorkloadModelChangeResponseDTO(change.WorkloadModelChangeId, created);
        }

        /// <summary>
        /// Enumerates the requested duty FTE values with labels for validation messages.
        /// </summary>
        /// <param name="request">Incoming workload-model-change request.</param>
        /// <returns>Named FTE values for each duty field.</returns>
        private static IEnumerable<(string Label, double FTE)> DutyFTEs(ImportWorkloadModelChangeDTO request)
        {
            yield return (nameof(WorkloadModelChange.ProjectWorkFTE), request.ProjectWorkFTE);
            yield return (nameof(WorkloadModelChange.BusinessAsUsualFTE), request.BusinessAsUsualFTE);
            yield return (nameof(WorkloadModelChange.PersonalDevelopmentFTE), request.PersonalDevelopmentFTE);
            yield return (nameof(WorkloadModelChange.StaffManagementFTE), request.StaffManagementFTE);
            yield return (nameof(WorkloadModelChange.ArchitectureFTE), request.ArchitectureFTE);
            yield return (nameof(WorkloadModelChange.ServiceManagementFTE), request.ServiceManagementFTE);
            yield return (nameof(WorkloadModelChange.ProjectManagementFTE), request.ProjectManagementFTE);
        }
    }
}
