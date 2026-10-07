// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Data.Enums.Attributes
{
    /// <summary>
    /// Add a hex colour string to an enum
    /// </summary>
    public class ColourAttribute : Attribute
    {
        public string BackgroundColourCode { get; }
        public string TextColourCode { get; }
        public string BackgroundColourCodeDark { get; }
        public string TextColourCodeDark { get; }

        /// <summary>
        /// Provide background and foreground (text) colour codes with optional dark mode versions.
        /// </summary>
        /// <param name="backgroundColourCode"></param>
        /// <param name="textColourCode"></param>
        /// <param name="backgroundColourCodeDark"></param>
        /// <param name="textColourCodeDark"></param>
        public ColourAttribute(string backgroundColourCode, string textColourCode = "#FFF", string backgroundColourCodeDark = null, string textColourCodeDark = null)
        {
            BackgroundColourCode = backgroundColourCode;
            TextColourCode = textColourCode;

            // If no dark mode variants provided then dark mode is assumed same as light mode
            if (backgroundColourCodeDark == null) BackgroundColourCodeDark = backgroundColourCode;
            else BackgroundColourCodeDark = backgroundColourCodeDark;
            if (textColourCodeDark == null) TextColourCodeDark = textColourCode;
            else TextColourCodeDark = textColourCodeDark;
        }
    }
}
