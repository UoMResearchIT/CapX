namespace PPMTool.Data.Context
{
    // Add profile data for application users by adding properties to the PPMToolUser class
    public class PPMToolUser
    {
        // Add custom role field
        public RoleType Role { get; set; }

        // Add friendly name field
        public string Name { get; set; }
    }
}
