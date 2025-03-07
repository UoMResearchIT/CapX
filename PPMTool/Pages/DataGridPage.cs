// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
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

        /// <summary>
        /// Reset error messages and the tracking of the entity being inserted or updated
        /// </summary>
        protected virtual void Reset()
        {
            entityToInsert = null;
            entityToUpdate = null;
            ErrorMessage = null;
        }

        /// <summary>
        /// Edit a row in the datagrid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected async virtual Task EditRow(T entity)
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
        protected async virtual Task SaveRow(T entity)
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

        /// <summary>
        /// Create an entity instance and add to datagrid
        /// </summary>
        /// <returns></returns>
        protected async virtual Task InsertRow()
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
            dataGridEntityService.Add(Context, entity);
        }

        /// <summary>
        /// Callback fired by the datagrid when a row is updated
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnUpdateRow(T entity)
        {
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");
            dataGridEntityService.Update(Context, entity);
        }
    }
}
