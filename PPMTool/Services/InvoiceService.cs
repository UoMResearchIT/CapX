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
    }
}
