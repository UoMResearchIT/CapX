namespace PPMTool.API.Attributes
{
    /// <summary>
    /// Attribute to tag a method in the API so that it invokes the appropriate operation filter during doc gen
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SkillTagShallowSchemaAttribute : Attribute
    {
    }
}
