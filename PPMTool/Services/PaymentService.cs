// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage payments
    /// </summary>
    public class PaymentService : BaseEntityService<Payment>
    {
        /// <summary>
        /// Get payments from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Payment> GetAll(PPMToolContext context)
        {
            return context.Payments
                .Include(x => x.Project)
                .Include(x => x.Invoice);
        }

        /// <summary>
        /// Adds a payment to the DB and optionally saves changes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="payment"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public override int Add(PPMToolContext context, Payment payment, bool commitChanges = true)
        {
            context.Payments.Add(payment);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return payment.PaymentId;
        }

        /// <summary>
        /// Updates an existing payment in the DB and optionally saves changes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="payment"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public override int Update(PPMToolContext context, Payment payment, bool commitChanges = true)
        {
            context.Payments.Update(payment);
            if (commitChanges)
            {
                CommitChanges(context);
            }
            return payment.PaymentId;
        }

        /// <summary>
        /// Delete a payment from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        public override void Delete(PPMToolContext context, Payment entity, bool commitChanges = true)
        {
            context.Payments.Remove(entity);
            if (commitChanges)
            {
                CommitChanges(context);
            }
        }

        /// <summary>
        /// Gets the funds received based on payments in the DB for the given project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public double GetFundsReceived(PPMToolContext context, int projectId)
        {
            return context.Payments
                .Where(x => x.Project.ProjectId == projectId)
                .RoundedSum(x => x.Value, 0);
        }

        /// <summary>
        /// Gets the invoice status by evaluating payments made against the invoice value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public InvoiceStatus GetInvoiceStatusFromPayments(PPMToolContext context, Invoice invoice)
        {
            var payments = context.Payments
                .Where(x => x.Invoice.InvoiceId == invoice.InvoiceId)
                .ToList();
            var totalPaid = payments.RoundedSum(x => x.Value, 0);
            var invoiceValue = Math.Round(invoice.Value, 0);
            if (totalPaid <= 0)
            {
                return InvoiceStatus.Unpaid;
            }
            else if (totalPaid < invoiceValue)
            {
                return InvoiceStatus.PartiallyPaid;
            }
            else
            {
                return InvoiceStatus.Paid;
            }
        }
    }
}
