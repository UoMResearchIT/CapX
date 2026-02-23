using System.Diagnostics;
using System.Linq.Dynamic.Core;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
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
        protected DialogService DialogService { get; set; }

        protected List<Faculty> Faculties = new();

        protected Faculty EditingFaculty = new Faculty();
        protected School EditingSchool = new School();

        protected bool IsAddingFaculty = false;
        protected bool IsAddingSchool = false;
        protected Faculty NewFaculty;
        protected School NewSchool;


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
            Faculties = FacultyService.GetAll(Context).OrderBy(f => f.Name).ToList();
        }

        /// <summary>
        /// Shared "Toggle Active" method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task ToggleActive((BaseOrgUnit unit, bool newValue) args)
        {
            var (unit, newValue) = args;

            switch (unit)
            {
                case Faculty faculty:
                    await ToggleFacultyActive(faculty, newValue);
                    break;

                case School school:
                    ToggleSchoolActive(school, newValue);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported org unit type: {unit.GetType().Name}");
            }
        }

        /// <summary>
        /// Shared method for cancelling adding new OrgUnit
        /// </summary>
        /// <param name="unit"></param>
        private void CancelOrgUnitAdd(BaseOrgUnit unit)
        {
            switch (unit)
            {
                case Faculty faculty:
                    EditingFaculty = null;
                    IsAddingFaculty = false;
                    break;

                case School school:
                    EditingSchool = null;
                    IsAddingSchool = false;
                    break;
            }

            StateHasChanged();
        }

        /// <summary>
        /// Shared method for saving a newly added OrgUnit
        /// </summary>
        /// <param name="unit"></param>
        protected void SaveOrgUnit(BaseOrgUnit unit)
        {
            switch (unit)
            {
                case Faculty faculty:
                    if (faculty.FacultyId == 0)
                    {
                        FacultyService.Add(Context, faculty);
                        Faculties.Add(faculty);
                        Faculties = Faculties.OrderBy(x => x.Name).ToList();
                    }
                    else
                    {
                        FacultyService.Update(Context, faculty);
                    }

                    EditingFaculty = null;
                    IsAddingFaculty = false;
                    faculty.InEditMode = false;
                    break;

                case School school:
                    if (school.SchoolId == 0)
                    {
                        SchoolService.Add(Context, school);
                        Faculty faculty = school.Faculty;
                        faculty.Schools.OrderBy(x => x.Name);
                    }
                    else
                    {
                        SchoolService.Update(Context, school);
                    }

                    EditingSchool = null;
                    IsAddingSchool = false;
                    school.InEditMode = false;
                    break;
            }
        }

        /// <summary>
        /// Allows cancellation of OrgUnit.editing.
        /// Restores the previous values and exits Edit Mode.
        /// </summary>
        /// <param name="unit"></param>
        protected void CancelOrgUnitEdit(BaseOrgUnit unit)
        {
            switch (unit)
            {
                case Faculty faculty:
                    FacultyService.RestoreModel(Context,ref unit);
                    unit.InEditMode = false;
                    EditingFaculty = null;
                    break;

                case School school:
                    SchoolService.RestoreModel(Context, ref unit);
                    unit.InEditMode = false;
                    EditingSchool = null;
                    break;
            }

        }

        /// <summary>
        /// Allows editing of an OrgUnit.
        /// Sets the view into Edit Mode.
        /// in the same Faculty
        /// </summary>
        /// <param name="school"></param>
        protected void EditOrgUnit(BaseOrgUnit unit)
        {
            switch(unit)
            {
                case Faculty faculty:
                    EditingFaculty = faculty;
                    faculty.InEditMode = true;
                    break;

                case School school:
                    EditingSchool = school;
                    foreach (School s in school.Faculty.Schools)
                    {
                        s.InEditMode = (s.SchoolId == EditingSchool.SchoolId ? true : false);
                    }
                    break;
            }

        }

        /// <summary>
        /// Creates a new Faculty entity to populate in the view, and sets
        /// the flag to show the relevant fields in the component
        /// </summary>
        protected void StartAddingFaculty()
        {
            NewFaculty = new Faculty
            {
                IsActive = true,
                Description = string.Empty
            };

            IsAddingFaculty = true;
        }



        /// <summary>
        /// Sets up a new School object ready to be altered and
        /// sets the view into Edit Mode
        /// </summary>
        /// <param name="faculty"></param>
        protected void StartAddingSchool(Faculty faculty)
        {
            NewSchool = new School
            {
                Faculty = faculty,
                IsActive = true,
                Description = string.Empty
            };

            IsAddingSchool = true;
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
        protected async Task ToggleFacultyActive(Faculty faculty, bool value)
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
    }
}