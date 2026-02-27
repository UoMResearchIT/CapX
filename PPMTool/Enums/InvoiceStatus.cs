// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;
using PPMTool.Enums.Attributes;
using Radzen;

namespace PPMTool.Enums
{
    /// <summary>
    /// State an invoice can be in
    /// </summary>
    public enum InvoiceStatus
    {
        [BadgeStyle(BadgeStyle.Danger)]
        Unpaid,
        [BadgeStyle(BadgeStyle.Warning)]
        [Description("Partially Paid")]
        PartiallyPaid,
        [BadgeStyle(BadgeStyle.Success)]
        Paid,
        [BadgeStyle(BadgeStyle.Light)]
        Cancelled
    }
}
