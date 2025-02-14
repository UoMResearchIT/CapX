using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddHistoricInvoiceAndPaymentRecords : Migration
    {
        private class InvoiceLine
        {
            public int InvId { get; set; }
            public int RTP { get; set; }
            public int RaiserId { get; set; }
            public double Amount { get; set; }
            public DateTime Requested { get; set; }
            public string InvoiceRef { get; set; }
            public InvoiceStatus Status { get; set; }
            public string InvoiceLink { get; set; }
            public string Comments { get; set; }
        }

        private class PaymentLine
        {
            public int InvId { get; set; }
            public double Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Comments { get; set; }
        }

        private string Clean(string initial)
        {
            return initial.Replace("\"\"", "**").Replace("\"", "").Replace("**", "\"\"").Replace("\r", "");
        }

        private InvoiceStatus MapToInvoiceStatus(string status)
        {
            switch (status)
            {
                case "Receipt Confirmed":
                    return InvoiceStatus.Paid;

                case "Cancelled":
                    return InvoiceStatus.Cancelled;
            }
            return InvoiceStatus.Unpaid;
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var filePath = $"./Migrations/Data/KnownInvoices.txt";
            var lines = File.ReadAllLines(filePath);
            List<InvoiceLine> invoiceLinesList = new List<InvoiceLine>();
            foreach (var line in lines)
            {
                // Split the line by the delimiter
                var values = line.Split('|');

                if (values.Length < 9 || Clean(values[0]) == "InvId")
                {
                    continue;
                }

                // Build the object
                var obj = new InvoiceLine()
                {
                    InvId = int.Parse(Clean(values[0])),
                    RTP = int.Parse(Clean(values[1])),
                    RaiserId = int.Parse(Clean(values[2])),
                    Amount = double.Parse(Clean(values[3])),
                    Requested = DateTime.ParseExact(Clean(values[4]), "dd MMMM yyyy", CultureInfo.InvariantCulture),
                    InvoiceRef = Clean(values[5]),
                    Status = MapToInvoiceStatus(Clean(values[6])),
                    InvoiceLink = Clean(values[7]),
                    Comments = Clean(values[8])
                };
            }

            Console.WriteLine($"** Have {invoiceLinesList.Count} rows from the invoices file!");

            // Now do the payments
            filePath = $"./Migrations/Data/KnownPayments.txt";
            lines = File.ReadAllLines(filePath);
            List<PaymentLine> paymentLinesList = new List<PaymentLine>();
            foreach (var line in lines)
            {
                // Split the line by the delimiter
                var values = line.Split('|');

                if (values.Length < 4 || Clean(values[0]) == "InvId")
                {
                    continue;
                }

                // Build the object
                var obj = new PaymentLine()
                {
                    InvId = int.Parse(Clean(values[0])),
                    Amount = double.Parse(Clean(values[1])),
                    TransactionDate = DateTime.ParseExact(Clean(values[2]), "dd MMMM yyyy", CultureInfo.InvariantCulture),
                    Comments = Clean(values[3])
                };

                paymentLinesList.Add(obj);
            }

            Console.WriteLine($"** Have {paymentLinesList.Count} rows from the payments file!");

            // Invoices
            var sqlScript = string.Empty;
            foreach (var invoice in invoiceLinesList)
            {
                sqlScript += $@"
                    INSERT INTO Invoices (InvoiceId, InvoiceReference, Status, KeyDate, Value, Description, ProjectId, InvoiceUrl)
                    SELECT '{invoice.InvId}', '{invoice.InvoiceRef}', '{(int)invoice.Status}', '{invoice.Requested:yyyy-MM-dd}', '{invoice.Amount}', '[Automatic Import from Old Tracker] {invoice.Comments}', ProjectId, '{invoice.InvoiceLink}'
                    FROM Projects
                    WHERE RTP = {invoice.RTP};
                ";
            }

            // Execute the SQL script
            if (!string.IsNullOrWhiteSpace(sqlScript))
            {
                migrationBuilder.Sql(sqlScript);
            }

            // Payments
            sqlScript = string.Empty;
            foreach (var payment in paymentLinesList)
            {
                // Need to lookup the project Id of the project 
                sqlScript += $@"
                    INSERT INTO Payments (InvoiceId, KeyDate, Value, Description, ProjectId)
                    SELECT '{payment.InvId}', '{payment.TransactionDate:yyyy-MM-dd}', '{payment.Amount}', '[Automatic Import from Old Tracker] {payment.Comments}', ProjectId
                    FROM Invoices
                    WHERE InvoiceId = {payment.InvId};
                ";
            }

            // Execute the SQL script
            if (!string.IsNullOrWhiteSpace(sqlScript))
            {
                migrationBuilder.Sql(sqlScript);
            }

            // Generate any other payments that are needed to balance the books
            sqlScript = $@"
                -- Loop through each record in the Projects table
                WITH ProjectPayments AS (
                    SELECT 
                        p.ProjectId,
                        p.FundsReceived,
                        COALESCE(SUM(py.Value), 0) AS TotalPayments
                    FROM 
                        Projects p
                    LEFT JOIN 
                        Payments py ON p.ProjectId = py.ProjectId
                    GROUP BY 
                        p.ProjectId
                )
                INSERT INTO Payments (InvoiceId, KeyDate, Value, Description, ProjectId)
                SELECT 
                    NULL AS InvoiceId,
                    DATE('now') AS KeyDate,
                    (pp.FundsReceived - pp.TotalPayments) AS Value,
                    '[Automatic Adjustment Payment] This payment has been created automatically as part of adding the finance tracking feature. This is due to the existing data in CapX for the project stating we had received more money for the project than just what was recorded on the old invoice tracker. These may well be salary costs and hence were not associated with an invoice and not tracked on the tracker.' AS Description,
                    pp.ProjectId
                FROM 
                    ProjectPayments pp
                WHERE 
                    pp.FundsReceived > pp.TotalPayments;
            ";
            if (!string.IsNullOrWhiteSpace(sqlScript))
            {
                migrationBuilder.Sql(sqlScript);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    DELETE FROM Invoices;
                    DELETE FROM Payments;
                "
            );
        }
    }
}
