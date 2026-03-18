using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Interfaces;
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

        /// <summary>
        /// Reset error messages and the tracking of the entity being inserted or updated
        /// </summary>
        protected virtual void Reset()
        {
            entityToInsert = null;
            entityToUpdate = null;
            ClearErrorMessage();
        }

        /// <summary>
        /// Edit a row in the datagrid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual async Task EditRow(T entity)
        {
            entityToUpdate = entity;
            LogInformation($"Edit row in view for <{entityToUpdate?.GetSensibleObjectName()}>");
            await dataGrid.EditRow(entity);
        }

        /// <summary>
        /// Update a row in the datagrid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual async Task SaveRow(T entity)
        {
            LogInformation($"Update row in view for <{entity?.GetSensibleObjectName()}>");
            Reset();
            await dataGrid.UpdateRow(entity);
        }

        /// <summary>
        /// Canacel the edit of a row in the datagrid
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void CancelEdit(T entity)
        {
            LogInformation($"Restore model and cancel edit row in view for <{entity?.GetSensibleObjectName()}>");
            Reset();
            dataGridEntityService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        /// <summary>
        /// Delete a row from the data grid (handles both existing row and one being added)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual async Task DeleteRow(T entity)
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

        /// <summary>
        /// Create an entity instance and add to datagrid
        /// </summary>
        /// <returns></returns>
        protected virtual async Task InsertRow()
        {
            entityToInsert = Activator.CreateInstance(typeof(T)) as T;
            LogInformation($"Add row in view for <{entityToInsert?.GetSensibleObjectName()}>");
            await dataGrid.InsertRow(entityToInsert);
        }

        /// <summary>
        /// Callback fired by the datagrid when a row is created
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnCreateRow(T entity)
        {
            LogInformation($"Add row to database for <{entity?.GetSensibleObjectName()}>");
            var duplicate = dataGridEntityService.Add(Context, entity);
            if (duplicate < 0)
            {
                AddDuplicateErrorMessage(entity);
            }
        }

        /// <summary>
        /// Callback fired by the datagrid when a row is updated
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnUpdateRow(T entity)
        {
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");
            var duplicate = dataGridEntityService.Update(Context, entity);
            if (duplicate < 0)
            {
                AddDuplicateErrorMessage(entity);
            }
        }

        /// <summary>
        /// Basic duplicate detected error message for a data grid page. Can be overridden as required.
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void AddDuplicateErrorMessage(T entity)
        {
            LogWarning($"Duplicate check failed for <{entity?.GetSensibleObjectName()}>");
            SetErrorMessage(new StatusMessage($"A record with the same values already exists. Please change the values to be unique and try again.", StatusMessage.MessageType.Error));
        }
    }
}
