// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;

namespace PPMTool.Enums.Attributes
{
    /// <summary>
    /// Converts an expiration DateTime to a value in days
    /// </summary>
    public class ExpirationInDaysAttribute : Attribute
    {
        public double Days { get; }

        public ExpirationInDaysAttribute(double days)
        {
            Days = days;
        }
    }
}
