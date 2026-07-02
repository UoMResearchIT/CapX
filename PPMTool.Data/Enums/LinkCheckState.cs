// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Data.Enums
{
    /// <summary>
    /// When checking whether a link gives a 404 we can represent its state with this
    /// </summary>
    public enum LinkCheckState
    {
        Pending,
        Fail,
        Success
    }
}
