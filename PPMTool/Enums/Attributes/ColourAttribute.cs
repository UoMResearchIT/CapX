// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;

namespace PPMTool.Enums.Attributes
{
    /// <summary>
    /// Add a hex colour string to an enum
    /// </summary>
    public class ColourAttribute : Attribute
    {
        public string BackgroundColourCode { get; }
        public string TextColourCode { get; }

        public ColourAttribute(string backgroundColourCode, string textColourCode = "#FFF")
        {
            BackgroundColourCode = backgroundColourCode;
            TextColourCode = textColourCode;
        }
    }
}
