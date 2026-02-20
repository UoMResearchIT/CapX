// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum School
    {
        [Description("None")]
        None,

        // FSE
        [Description("School of Engineering")]
        SE,
        [Description("School of Natural Sciences")]
        SNS,

        // FBMH
        [Description("School of Biological Sciences")]
        SBS,
        [Description("School of Medical Sciences")]
        SMS,
        [Description("School of Health Sciences")]
        SHS,

        // FHUMS
        [Description("Alliance Manchester Business School")]
        AMBS,
        [Description("School of Arts, Languages and Cultures")]
        SALC,
        [Description("School of Environment, Education and Development")]
        SEED,
        [Description("School of Social Sciences")]
        SSS
    }
}
