using System;

namespace PPMTool.Enums
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
