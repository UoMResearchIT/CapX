// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;

namespace PPMTool.Enums
{
    /// <summary>
    /// An abbreviated description if the description attribute is already in use
    /// </summary>
    public class ShortDescriptionAttribute : Attribute
    {
        public string Value { get; }

        public ShortDescriptionAttribute(string value)
        {
            Value = value;
        }
    }
}
