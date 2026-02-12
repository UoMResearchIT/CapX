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

        protected List<Faculty> Faculties = new();

        protected Faculty EditingFaculty;
        protected School EditingSchool;

        protected override async Task OnInitializedAsync()
        {
            LoadData();
        }

        private void LoadData()
        {
            Faculties = FacultyService.GetAll(Context).OrderBy(x => x.Order).ThenBy(x => x.Name).ToList();
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

            await Context.SaveChangesAsync();
            EditingFaculty = null;
            LoadData();
        }

        protected void CancelFacultyEdit()
        {
            if (EditingFaculty?.FacultyId == 0)
                Faculties.Remove(EditingFaculty);

            EditingFaculty = null;
        }

        protected async Task DeactivateFaculty(Faculty faculty)
        {
            faculty.IsActive = false;
            FacultyService.Update(Context, faculty);
            await Context.SaveChangesAsync();
            LoadData();
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

            await Context.SaveChangesAsync();
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

        protected async Task DeactivateSchool(Faculty faculty, School school)
        {
            school.IsActive = false;
            SchoolService.Update(Context, school);
            await Context.SaveChangesAsync();
            LoadData();
        }

        protected async Task ReactivateSchool(Faculty faculty, School school)
        {
            school.IsActive = true;
            SchoolService.Update(Context, school);
            await Context.SaveChangesAsync();
            LoadData();
        }

        // ---------------- VISUAL STYLING ----------------

        protected string GetFacultyIcon(Faculty f) =>
            f.IsActive ? "check_circle" : "block";

        protected string GetFacultyStyle(Faculty f) =>
            f.IsActive ? "" : "opacity:0.5; text-decoration: line-through;";

        protected string GetSchoolStyle(School s) =>
            s.IsActive ? "" : "opacity:0.5; text-decoration: line-through;";
    }


}
    //public partial class ManageOrgUnits : DataGridPage<Faculty>
    //{
    //    [Inject]
    //    private FacultyService FacultyService { get; set; }

    //    private int count;

    //    protected override void OnInitialized()
    //    {
    //        base.OnInitialized();
    //        EditAuthorised = ActiveUserRoleType == RoleType.Superuser;
    //        dataGridEntityService = FacultyService;
    //        Loading = true;
    //        EnqueueLoadData(GetLoadTask);
    //        LogInformation($"Viewing Organisational Units");
    //    }

    //    protected override async Task SaveRow(Faculty entity)
    //    {
    //        if (IsDuplicatedFaculty(entity)) return;
    //        await base.SaveRow(entity);
    //    }

    //    protected override async Task DeleteRow(Faculty entity)
    //    {
    //        if (await DialogService.Confirm($"You are about to delete tag {entity.GetSensibleObjectName()}.", "Delete Tag") ?? false)
    //        {
    //            await base.DeleteRow(entity);

    //            // Remove from data grid
    //            dataGridEntityService.Delete(Context, entity);
    //            LogInformation($"Deleted skills tag {entity.GetSensibleObjectName()}");
    //            await dataGrid.Reload();
    //        }
    //    }

    //    /// <summary>
    //    /// Method to detect a duplicate on save or update and display error message
    //    /// </summary>
    //    /// <param name="entity"></param>
    //    /// <returns></returns>
    //    private bool IsDuplicatedFaculty(Faculty entity)
    //    {
    //        if (FacultyService.DuplicateDetected(Context, entity))
    //        {
    //            SetErrorMessage(new StatusMessage("An entry with the same name or controlled name already exists.", StatusMessage.MessageType.Error));
    //            return true;
    //        }
    //        ClearErrorMessage();
    //        return false;
    //    }

    //    /// <summary>
    //    /// Method fired when a column is filtered or sorted to allow us to custom filter or sort
    //    /// </summary>
    //    /// <param name="args"></param>
    //    private void LoadDataGrid(LoadDataArgs args)
    //    {
    //        // Order by name by default
    //        IQueryable<Faculty> query = FacultyService.GetAll(Context).OrderBy(x => x.Name).AsQueryable();

    //        // Assign to grid source
    //        var data = query.ToList();
    //        count = data.Count;
    //        dataGridEntities = data;
    //        Loading = false;

    //        Debug.WriteLine($"** {data.Count()} faculties loaded. {dataGridEntities.Count()} displayed.");
    //    }

    //    /// <summary>
    //    /// Returns a standard task to get the data for the grid
    //    /// </summary>
    //    /// <returns></returns>
    //    private Task GetLoadTask()
    //    {
    //        return Task.Run(() =>
    //        {
    //            LoadDataGrid(new LoadDataArgs());
    //        })
    //            .ContinueWith(t =>
    //            {
    //                InvokeAsync(() =>
    //                {
    //                    Loading = false;
    //                    StateHasChanged();
    //                });
    //            });
    //    }

    //}
//}