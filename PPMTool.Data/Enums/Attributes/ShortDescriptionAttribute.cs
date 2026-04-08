namespace PPMTool.Data.Enums.Attributes
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
