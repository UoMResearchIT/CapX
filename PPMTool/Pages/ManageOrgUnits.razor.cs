using System.Diagnostics;
using System.Linq.Dynamic.Core;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
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

        protected Faculty EditingFaculty;
        protected School EditingSchool;
        protected bool IsAddingSchool = false;
        protected bool IsAddingFaculty = false;
        protected School NewSchool;
        protected Faculty NewFaculty;

        protected override async Task OnInitializedAsync()
        {
            LoadData();
        }

        private void LoadData()
        {
            Faculties = FacultyService.GetAll(Context).OrderBy(f => f.Name).ToList();
        }

        protected string GetFacultyCss(Faculty f) => f.IsActive ? "" : "inactive";
        protected string GetSchoolCss(School s) => s.IsActive ? "" : "inactive";


        // ---------------- FACULTY CRUD ----------------

        protected void StartAddingFaculty()
        {
            NewFaculty = new Faculty
            {
                IsActive = true,
                Description = string.Empty
            };

            IsAddingFaculty = true;
        }

        protected void EditFaculty(Faculty faculty)
        {
            EditingFaculty = faculty;
        }

        protected async Task SaveFaculty(Faculty faculty)
        {
            if (faculty.FacultyId == 0)
                FacultyService.Add(Context, faculty);
            else
                FacultyService.Update(Context, faculty);

            EditingFaculty = null;
            NewFaculty = null;
            IsAddingFaculty = false;
            LoadData();
        }

        protected void CancelFacultyEdit(Faculty entity)
        {
            if (EditingFaculty?.FacultyId == 0)
                Faculties.Remove(EditingFaculty);
            FacultyService.RestoreModel(Context, ref entity);

            EditingFaculty = null;
        }

        protected void CancelNewFacultyAdd(Faculty entity)
        {
            IsAddingFaculty = false;
            NewFaculty = null;
        }

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

        protected void EditSchool(School school)
        {
            EditingSchool = school;
        }

        protected async Task SaveSchool(Faculty faculty, School school)
        {
            if (school.SchoolId == 0)
                SchoolService.Add(Context, school);
            else
                SchoolService.Update(Context, school);

            EditingSchool = null;
            NewSchool = null;
            IsAddingSchool = false;
            LoadData();
        }

        protected void CancelSchoolEdit(School entity)
        {
            if (EditingSchool?.SchoolId == 0)
            {
                var parent = Faculties.First(f => f.FacultyId == EditingSchool.Faculty.FacultyId);
                parent.Schools.Remove(EditingSchool);
            }

            SchoolService.RestoreModel(Context, ref entity);
            EditingSchool = null;
        }

        protected void CancelNewSchoolAdd(School entity)
        {
            IsAddingSchool = false;
            NewSchool = null;
        }

        protected async Task ToggleSchoolActive(School school, bool value)
        {
            school.IsActive = value;
            FacultyService.Update(Context, school.Faculty);
        }
    }
}