// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// The available options for validity of an API key
    /// </summary>
    public enum ApiKeyExpirationOptions
    {
        [ExpirationInDays(0.000694)]
        [Description("1 Minute")]
        OneMin,
        [ExpirationInDays(1)]
        [Description("1 Day")]
        OneDay,
        [ExpirationInDays(30)]
        [Description("30 Days")]
        ThirtyDays,
        [ExpirationInDays(90)]
        [Description("90 Days")]
        NintyDays
    }
}
