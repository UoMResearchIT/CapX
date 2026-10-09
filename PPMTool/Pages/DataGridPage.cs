// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Interfaces;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public abstract class DataGridPage<T> : BasePage where T : class, ILoggableObject
    {
        protected RadzenDataGrid<T> dataGrid;
        protected IList<T> dataGridEntities;
        protected T entityToInsert;
        protected T entityToUpdate;
        protected IEntityService<T> dataGridEntityService;
        private bool rowSaveFailed;

        [Inject]
        protected DialogService DialogService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>
        /// Marks the current row save operation as failed.
        /// The row will be returned to edit mode.
        /// </summary>
        protected void MarkRowSaveFailed()
        {
            rowSaveFailed = true;
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
        /// Update a row in the datagrid.
        /// If persistence fails, return the row to edit mode.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual async Task SaveRow(T entity)
        {
            LogInformation($"Update row in view for <{entity?.GetSensibleObjectName()}>");

            ClearErrorMessage();
            rowSaveFailed = false;

            // Check whether insert or update
            var isInsert = ReferenceEquals(entityToInsert, entity);

            // Complete the RadzenDataGrid update
            await dataGrid.UpdateRow(entity);

            // UpdateRow takes the row out of edit mode, so restore both
            // our tracking state and the Radzen edit state.
            if (rowSaveFailed)
            {
                if (isInsert)
                {
                    entityToInsert = entity;
                    entityToUpdate = null;
                }
                else
                {
                    entityToInsert = null;
                    entityToUpdate = entity;
                }

                await dataGrid.EditRow(entity);
                return;
            }

            Reset();
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
            var isInDataSource = dataGridEntities.Contains(entity);
            var isTrackedEditRow = ReferenceEquals(entityToInsert, entity) || ReferenceEquals(entityToUpdate, entity);

            if (isInDataSource)
            {
                LogInformation($"Delete row in data grid source for <{entity?.GetSensibleObjectName()}>");
                dataGridEntities.Remove(entity);
            }
            else if (isTrackedEditRow)
            {
                LogInformation($"Cancel edit row in view for <{entity?.GetSensibleObjectName()}>");
                dataGrid.CancelEditRow(entity);
            }
            else
            {
                LogWarning($"Ignoring delete request for stale row <{entity?.GetSensibleObjectName()}>.");
            }

            Reset();

            if (dataGrid != null && (isInDataSource || isTrackedEditRow))
            {
                await dataGrid.Reload();
            }
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

            var result = dataGridEntityService.Add(Context, entity);

            if (result < 0)
            {
                MarkRowSaveFailed();
                AddDuplicateErrorMessage(entity);
                return;
            }

            entityToInsert = null;
        }

        /// <summary>
        /// Callback fired by the datagrid when a row is updated
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnUpdateRow(T entity)
        {
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");

            var result = dataGridEntityService.Update(Context, entity);

            if (result < 0)
            {
                MarkRowSaveFailed();
                AddDuplicateErrorMessage(entity);
                return;
            }

            entityToUpdate = null;
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
