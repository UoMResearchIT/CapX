namespace PPMTool.Enums
{
    public enum RoleType
    {
        // Specify the int manually as the roles have changed for the ITS implementation.
        // Saves having to remap anything in the database which references the role id
        Contractor = 2,
        Manager = 4,
        Superuser = 5
    }
}
