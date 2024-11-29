using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public abstract class DataGridPage<T> : BasePage where T : class, ILoggableClass
    {
        protected RadzenDataGrid<T> dataGrid;
        protected IList<T> dataGridEntities;
        protected T entityToInsert;
        protected T entityToUpdate;
        protected IEntityService<T> dataGridEntityService;

        [Inject]
        protected DialogService DialogService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected virtual void Reset()
        {
            entityToInsert = null;
            entityToUpdate = null;
            ErrorMessage = null;
        }

        protected async virtual Task EditRow(T entity)
        {
            LogInformation($"Edit row in view for <{entity?.GetSensibleObjectName()}>");
            entityToUpdate = entity;
            await dataGrid.EditRow(entity);
        }

        protected async virtual Task SaveRow(T entity)
        {
            LogInformation($"Update row in view for <{entity?.GetSensibleObjectName()}>");
            await dataGrid.UpdateRow(entity);
        }

        protected virtual void CancelEdit(T entity)
        {
            LogInformation($"Restore model and cancel edit row in view for <{entity?.GetSensibleObjectName()}>");
            Reset();
            dataGridEntityService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        protected async virtual Task DeleteRow(T entity)
        {
            Reset();

            if (dataGridEntities.Contains(entity))
            {
                LogInformation($"Delete row in data grid source for <{entity?.GetSensibleObjectName()}>");
                dataGridEntities.Remove(entity);
            }
            else
            {
                LogInformation($"Cancel edit row in view for <{entity?.GetSensibleObjectName()}>");
                dataGrid.CancelEditRow(entity);
            }
            await dataGrid.Reload();
        }

        protected async virtual Task InsertRow()
        {
            entityToInsert = Activator.CreateInstance(typeof(T)) as T;
            LogInformation($"Add row in view for <{entityToInsert?.GetSensibleObjectName()}>");
            await dataGrid.InsertRow(entityToInsert);
        }

        protected virtual void OnCreateRow(T entity)
        {
            Reset();
            LogInformation($"Add row to database for <{entity?.GetSensibleObjectName()}>");
            dataGridEntityService.Add(Context, entity);
        }

        protected virtual void OnUpdateRow(T entity)
        {
            Reset();
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");
            dataGridEntityService.Update(Context, entity);
        }
    }
}
