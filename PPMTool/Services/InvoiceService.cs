using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
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
        /// Get payments from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Payment> GetAllPayments(PPMToolContext context)
        {
            return context.Payments
                .Include(x => x.Project)
                .Include(x => x.Invoice);
        }

        /// <summary>
        /// Gets the funds received based on payments in the DB for the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public double GetFundsReceived(PPMToolContext context, int projectId)
        {
            return context.Payments.Where(x => x.Project.ProjectId == projectId).Sum(x => x.Value);
        }
    }
}
