using System;

namespace PPMTool.Enums
{
    /// <summary>
    /// Reference an icon by its name as a string
    /// </summary>
    public class IconAttribute : Attribute
    {
        public string Name { get; private set; }

        public IconAttribute(string name)
        {
            Name = name;
        }
    }
}
