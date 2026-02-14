using System.Diagnostics;
using System.Linq.Dynamic.Core;
using DocumentFormat.OpenXml.InkML;
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

        protected override async Task OnInitializedAsync()
        {
            LoadData();
        }

        private void LoadData()
        {
            Faculties = FacultyService.GetAll(Context).ToList();
        }

        protected string GetFacultyCss(Faculty f) => f.IsActive ? "" : "inactive";
        protected string GetSchoolCss(School s) => s.IsActive ? "" : "inactive";


        // ---------------- FACULTY CRUD ----------------

        protected void AddFaculty()
        {
            var f = new Faculty { IsActive = true };
            Faculties.Insert(0, f);
            EditingFaculty = f;
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
            LoadData();
        }

        protected void CancelFacultyEdit()
        {
            if (EditingFaculty?.FacultyId == 0)
                Faculties.Remove(EditingFaculty);

            EditingFaculty = null;
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

        protected void AddSchool(Faculty faculty)
        {
            var s = new School { Faculty = faculty, IsActive = true };
            faculty.Schools.Add(s);
            EditingSchool = s;
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
            LoadData();
        }

        protected void CancelSchoolEdit()
        {
            if (EditingSchool?.SchoolId == 0)
            {
                var parent = Faculties.First(f => f.FacultyId == EditingSchool.Faculty.FacultyId);
                parent.Schools.Remove(EditingSchool);
            }

            EditingSchool = null;
        }

        protected async Task ToggleSchoolActive(School school, bool value)
        {
            school.IsActive = value;
            FacultyService.Update(Context, school.Faculty);
        }
    }
}