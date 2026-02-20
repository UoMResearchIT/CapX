// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Faculty / divions to which a project belongs
    /// </summary>
    public enum Faculty
    {
        None,
        [Description("Research IT / Internal")]
        Internal,
        [Description("Professional Services and Cultural Institutions")]
        PSCI,
        [Description("Biology, Medicine and Health")]
        FBMH,
        [Description("Humanities")]
        FHUMS,
        [Description("Science and Engineering")]
        FSE,
        [Description("Research Lifecycle Programme")]
        RLP,
        [Description("Commercial / External")]
        External,
        [Description("Cross-Faculty Research Institutes")]
        ResInst
    }
}
