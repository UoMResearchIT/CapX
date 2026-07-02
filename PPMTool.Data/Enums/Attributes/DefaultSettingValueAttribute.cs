// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Data.Enums.Attributes
{
    /// <summary>
    /// An string value of a setting to be used as a default value when the system is initialised.
    /// </summary>
    public class DefaultSettingValueAttribute : Attribute
    {
        public string Value { get; }

        public DefaultSettingValueAttribute(string value)
        {
            Value = value;
        }
    }
}
