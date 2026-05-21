// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageOrgUnits : BasePage
    {
        [Inject]
        private FacultyService FacultyService { get; set; }

        [Inject]
        private SchoolService SchoolService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        private List<Faculty> faculties = new();
        private bool isAddingFaculty = false;
        private bool isAddingSchool = false;
        private Faculty newFaculty;
        private School newSchool;


        protected override void OnInitialized()
        {
            base.OnInitialized();
            LoadData();
        }

        /// <summary>
        /// Data loading method. Gets the faculties (ordered by name) and deeploads the schools.
        /// </summary>
        private void LoadData()
        {
            faculties = FacultyService.GetAll(Context).OrderBy(f => f.Name).ToList();
        }

        /// <summary>
        /// Shared method for cancelling adding new OrgUnit
        /// </summary>
        /// <param name="unit"></param>
        private void OrgUnitAddCancelled<T>(T unit) where T : BaseOrgUnit
        {
            switch (unit)
            {
                case Faculty faculty:
                    isAddingFaculty = false;
                    break;

                case School school:
                    isAddingSchool = false;
                    break;
            }
        }

        /// <summary>
        /// Shared method for saving a newly added OrgUnit
        /// </summary>
        /// <param name="unit"></param>
        protected void OrgUnitSuccessfulAdd(BaseOrgUnit unit)
        {
            // Update the state of the page
            switch (unit)
            {
                case Faculty faculty:
                    isAddingFaculty = false;
                    break;

                case School school:
                    isAddingSchool = false;
                    break;
            }

            // Refresh the list
            LoadData();
        }

        /// <summary>
        /// Handler for when a save fails
        /// </summary>
        /// <param name="unit"></param>
        private void OrgUnitEditFailed(BaseOrgUnit unit)
        {
            NotifyOfUniquenessError(unit);
        }

        /// <summary>
        /// Creates a new Faculty entity to populate in the view, and sets
        /// the flag to show the relevant fields in the component
        /// </summary>
        protected void StartAddingFaculty()
        {
            newFaculty = new Faculty
            {
                IsActive = true,
            };

            isAddingFaculty = true;
        }

        /// <summary>
        /// Sets up a new School object ready to be altered and
        /// sets the view into Edit Mode
        /// </summary>
        /// <param name="faculty"></param>
        protected void StartAddingSchool(Faculty faculty)
        {
            newSchool = new School
            {
                Faculty = faculty,
                IsActive = true,
            };

            isAddingSchool = true;
        }

        /// <summary>
        /// Shared "Toggle Active" method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task ToggleActiveAsync((BaseOrgUnit unit, bool newValue) args)
        {
            var (unit, newValue) = args;

            switch (unit)
            {
                case Faculty faculty:
                    await ToggleFacultyActiveAsync(faculty, newValue);
                    break;

                case School school:
                    // Reactivate the parent Faculty if the School is made active
                    if (newValue == true)
                    {
                        await ToggleFacultyActiveAsync(school.Faculty, true);
                    }
                    ToggleSchoolActive(school, newValue);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported org unit type: {unit.GetType().Name}");
            }
        }

        /// <summary>
        /// Toggles the active status of a Faculty. 
        /// Checks if there are schools and performs different
        /// actions based on this (prompts user to confirm marking
        /// all schools as inactive too).
        /// </summary>
        /// <param name="faculty"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected async Task ToggleFacultyActiveAsync(Faculty faculty, bool value)
        {
            if (!value)
            {
                // Set default value
                var confirmed = false;

                // Show dialogue box if sub-units within this organisational unit
                if (faculty.Schools.Count > 0)
                {
                    confirmed = (bool)await DialogService.Confirm(
                                    $"Are you sure you want to deactivate '{faculty.Name}'? All linked items within it will be deactivated too!",
                                    "Confirm Deactivation",
                                    new ConfirmOptions()
                                    {
                                        OkButtonText = "Deactivate",
                                        CancelButtonText = "Cancel"
                                    });
                }
                else
                {
                    // No sub-units so just set confirmation to true
                    confirmed = true;
                }

                if (confirmed != true)
                {
                    // Revert switch as not confirmed
                    faculty.IsActive = true;
                    return;
                }
                else
                {
                    faculty.IsActive = false;

                    foreach (School s in faculty.Schools)
                    {
                        s.IsActive = false;
                    }
                }
            }

            faculty.IsActive = value;
            FacultyService.Update(Context, faculty);
        }


        /// <summary>
        /// Toggles the active status of a school entity
        /// </summary>
        /// <param name="school"></param>
        /// <param name="value"></param>
        protected void ToggleSchoolActive(School school, bool value)
        {
            school.IsActive = value;
            FacultyService.Update(Context, school.Faculty);
        }

        /// <summary>
        /// Wrapper to present an uniqueness error message
        /// </summary>
        /// <param name="unit"></param>
        private void NotifyOfUniquenessError(BaseOrgUnit unit)
        {
            ShowNotification(new CapXNotificationMessage
            {
                Summary = "Duplicate Detected!",
                Detail = $"The {GetSetting((unit is School ? SettingType.OrgUnitLower : SettingType.OrgUnitUpper))} name and code must be unique!"
            });
        }
    }
}