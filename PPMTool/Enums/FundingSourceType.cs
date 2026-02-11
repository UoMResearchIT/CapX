// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum FundingSourceType
    {
        [Description("Directly Allocated")]
        [ShortDescription("DA")]
        DA,
        [Description("Directly Incurred")]
        [ShortDescription("DI")]
        DI,
        [Description("Other")]
        [ShortDescription("Other")]
        Other
    }
}
