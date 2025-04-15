using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for managing the DB table associated with invoices
    /// </summary>
    public class InvoiceService : BaseEntityService<Invoice>
    {
        public override int Add(PPMToolContext context, Invoice entity, bool commitChanges = true)
        {
            context.Invoices.Add(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
            return entity.InvoiceId;
        }

        public override void Delete(PPMToolContext context, Invoice entity, bool commitChanges = true)
        {
            context.Invoices.Remove(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
        }

        public override IEnumerable<Invoice> GetAll(PPMToolContext context)
        {
            return context.Invoices
                .Include(x => x.Project)
                .Include(x => x.Payments);
        }

        public override int Update(PPMToolContext context, Invoice entity, bool commitChanges = true)
        {
            context.Invoices.Update(entity);
            if (commitChanges)
            {
                context.SaveChanges();
            }
            return entity.InvoiceId;
        }

        /// <summary>
        /// Gets the funds rquested based on invoices in the DB for the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public double GetFundsRequested(PPMToolContext context, int projectId)
        {
            return context.Invoices.Where(x => x.Project.ProjectId == projectId && x.Status != Enums.InvoiceStatus.Cancelled).RoundedSum(x => x.Value, 0);
        }
    }
}
