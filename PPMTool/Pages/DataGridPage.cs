using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PPMTool.Data.Context;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public interface ILoggableClass
    {
        public abstract string GetSensibleObjectName();
    }

    public abstract class DataGridPage<T> : BasePage where T : class, ILoggableClass
    {
        protected RadzenDataGrid<T> dataGrid;
        protected IList<T> dataGridEntities;
        protected T entityToInsert;
        protected T entityToUpdate;
        protected IEntityService<T> dataGridEntityService;
        protected PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();
        }

        protected virtual void Reset()
        {
            LogInformation($"Reset in-line edit <{entityToInsert?.GetSensibleObjectName()}>");
            entityToInsert = null;
            entityToUpdate = null;
        }

        protected async virtual Task EditRow(T entity)
        {
            LogInformation($"Edit row for <{entity?.GetSensibleObjectName()}>");
            entityToUpdate = entity;
            await dataGrid.EditRow(entity);
        }

        protected async virtual Task SaveRow(T entity)
        {
            LogInformation($"Save row for <{entity?.GetSensibleObjectName()}>");
            await dataGrid.UpdateRow(entity);
        }

        protected virtual void CancelEdit(T entity)
        {
            LogInformation($"Cancel edit row for <{entity?.GetSensibleObjectName()}>");
            Reset();
            dataGridEntityService.RestoreModel(context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        protected async virtual Task DeleteRow(T entity)
        {
            Reset();

            if (dataGridEntities.Contains(entity))
            {
                LogInformation($"Delete row for <{entity?.GetSensibleObjectName()}>");
                dataGridEntities.Remove(entity);
            }
            else
            {
                dataGrid.CancelEditRow(entity);
            }
            await dataGrid.Reload();
        }

        protected async virtual Task InsertRow()
        {
            entityToInsert = Activator.CreateInstance(typeof(T)) as T;
            await dataGrid.InsertRow(entityToInsert);
        }

        protected virtual void OnCreateRow(T entity)
        {
            Reset();
            LogInformation($"Create row for <{entity?.GetSensibleObjectName()}>");
            dataGridEntityService.Add(context, entity);
        }

        protected virtual void OnUpdateRow(T entity)
        {
            Reset();
            LogInformation($"Update row for <{entity?.GetSensibleObjectName()}>");
            dataGridEntityService.Update(context, entity);
        }
    }
}
