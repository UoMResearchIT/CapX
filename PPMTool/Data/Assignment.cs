using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a sub task but with additional information about the project to which it belongs.
    /// </summary>
    public class Assignment
    {
        public SubTask SubTask { get; private set; }

        public ProjectStatus ProjectStatus { get; private set; }

        public Assignment(SubTask subTask, ProjectStatus projectStatus)
        {
            SubTask = subTask;
            ProjectStatus = projectStatus;
        }
    }
}
