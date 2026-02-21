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

        // ---------------- FACULTY CRUD ----------------

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
        /// Sets the flag on the relevant entity so that the view
        /// shows the fields for editing it. Resets the flag on all other entities.
        /// </summary>
        /// <param name="faculty"></param>
        protected void EditFaculty(Faculty faculty)
        {
            EditingFaculty = faculty;
            foreach (Faculty f in Faculties)
            {
                f.InEditMode = (f.FacultyId == EditingFaculty.FacultyId ? true : false);
            }
        }

        /// <summary>
        /// Saves adjustments made to the Faculty entity being edited
        /// </summary>
        /// <param name="entity"></param>
        protected void SaveFaculty(Faculty entity)
        {
            if (entity.FacultyId == 0)
            {
                FacultyService.Add(Context, entity);
                Faculties.Add(entity);
                Faculties = Faculties.OrderBy(x => x.Name).ToList();
            }
            else
            {
                FacultyService.Update(Context, entity);
            }

            EditingFaculty = null;
            IsAddingFaculty = false;
            entity.InEditMode = false;
        }

        /// <summary>
        /// Cancels the editing in progress. 
        /// Restores the previous values and exits Edit Mode.
        /// </summary>
        /// <param name="entity"></param>
        protected void CancelFacultyEdit(Faculty entity)
        {
            FacultyService.RestoreModel(Context, ref entity);
            entity.InEditMode = false;
            EditingFaculty = null;
        }

        /// <summary>
        /// Cancels the adding a new faculty process. 
        /// </summary>
        /// <param name="entity"></param>
        protected void CancelNewFacultyAdd(Faculty entity)
        {
            IsAddingFaculty = false;
            NewFaculty = null;
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


        // ---------------- SCHOOL CRUD ----------------

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
        /// Sets the view into Edit Mode and unsets others schools 
        /// in the same Faculty
        /// </summary>
        /// <param name="school"></param>
        protected void EditSchool(School school)
        {
            EditingSchool = school;
            foreach (School s in school.Faculty.Schools)
            {
                s.InEditMode = (s.SchoolId == EditingSchool.SchoolId ? true : false);
            }
        }

        /// <summary>
        /// Saves/Updates a School in the db
        /// </summary>
        /// <param name="school"></param>
        protected void SaveSchool(School entity)
        {
            if (entity.SchoolId == 0)
            {
                SchoolService.Add(Context, entity);
                Faculty faculty = entity.Faculty;
                faculty.Schools.OrderBy(x => x.Name);
            }
            else
            {
                SchoolService.Update(Context, entity);
            }

            EditingSchool = null;
            IsAddingSchool = false;
            entity.InEditMode = false;
        }

        /// <summary>
        /// Cancels Edit Mode for the school and restores the previous data
        /// </summary>
        /// <param name="entity"></param>
        protected void CancelSchoolEdit(School entity)
        {
            SchoolService.RestoreModel(Context, ref entity);
            entity.InEditMode = false;
            EditingSchool = null;
        }

        /// <summary>
        /// Cancels the adding a enw school process
        /// </summary>
        /// <param name="entity"></param>
        protected void CancelNewSchoolAdd(School entity)
        {
            IsAddingSchool = false;
            NewSchool = null;
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