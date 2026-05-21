using System.ComponentModel;

namespace PPMTool.Data.Enums
{
    public enum SkillProficiency
    {
        [Description("Not Yet Rated")]
        NotRated,
        None,
        Beginner,
        Intermediate,
        Advanced,
        Guru
    }
}
