using PPMTool.Enums.Attributes;

namespace PPMTool.Enums
{
    public enum SkillRareness
    {
        [Icon("common")]
        [Colour("", "#888")]
        Common,
        [Icon("uncommon")]
        [Colour("", "#00f")]
        Uncommon,
        [Icon("rare")]
        [Colour("", "#396")]
        Rare,
        [Icon("epic")]
        [Colour("", "#609")]
        Epic,
        [Icon("legendary")]
        [Colour("", "#f00")]
        Legendary
    }
}
