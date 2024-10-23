using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public abstract class AddPersonProperty<T> : DataGridPage<T> where T : PersonProperty
    {
        [Inject]
        public PersonService PersonService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        protected Person personModel;

        protected override void CancelEdit(T entity)
        {
            LogInformation($"Cancel row edit for {entity?.GetSensibleObjectName()}");
            Reset();
            PersonService.RestoreModel(context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        protected override async Task InsertRow()
        {
            entityToInsert = Activator.CreateInstance(typeof(T)) as T;
            entityToInsert.Person = personModel;
            await Task.CompletedTask;
        }

        protected override void OnCreateRow(T entity)
        {
            LogInformation($"Added row for {entity?.GetSensibleObjectName()}");
            entity.Person = personModel;
            dataGridEntities.Add(entity);
            entityToInsert = null;
        }

        protected override void OnUpdateRow(T entity)
        {
            Reset();
        }
    }
}
