// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for managing the DB table associated with invoices
    /// </summary>
    public class InvoiceService : BaseEntityService<Invoice>
    {
        public InvoiceService(ILogger<InvoiceService> logger) : base(logger)
        {
        }

        public override int Add(PPMToolContext context, Invoice entity, bool commitChanges = true)
        {
            context.Invoices.Add(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return entity.InvoiceId;
        }

        public override void Delete(PPMToolContext context, Invoice entity, bool commitChanges = true)
        {
            context.Invoices.Remove(entity);
            if (commitChanges)
            {
                CommitChanges(context);
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
                CommitChanges(context);
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
            return context.Invoices
                .Where(x => x.Project.ProjectId == projectId && x.Status != InvoiceStatus.Cancelled)
                .RoundedSum(x => x.Value, 0);
        }

        /// <summary>
        /// Gets the funds requested from non-cancelled invoices in the given financial year.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <param name="financialYear"></param>
        /// <returns></returns>
        public double GetFundsRequestedForFinancialYear(PPMToolContext context, int projectId, int financialYear)
        {
            var financialYearStart = new DateTime(financialYear, 8, 1);
            var financialYearEnd = new DateTime(financialYear + 1, 7, 31);

            return context.Invoices
                .Where(x => x.Project.ProjectId == projectId
                    && x.Status != InvoiceStatus.Cancelled
                    && x.KeyDate.Date >= financialYearStart.Date
                    && x.KeyDate.Date <= financialYearEnd.Date)
                .RoundedSum(x => x.Value, 0);
        }

        /// <summary>
        /// Whether there is at least one non-cancelled invoice for the project in the given financial year.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <param name="financialYear"></param>
        /// <returns></returns>
        public bool HasInvoiceInFinancialYear(PPMToolContext context, int projectId, int financialYear)
        {
            var financialYearStart = new DateTime(financialYear, 8, 1);
            var financialYearEnd = new DateTime(financialYear + 1, 7, 31);

            return context.Invoices
                .Any(x => x.Project.ProjectId == projectId
                    && x.Status != InvoiceStatus.Cancelled
                    && x.KeyDate.Date >= financialYearStart.Date
                    && x.KeyDate.Date <= financialYearEnd.Date);
        }
    }
}
