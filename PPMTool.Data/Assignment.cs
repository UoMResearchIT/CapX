using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data
{
    /// <summary>
    /// Assignment representing information used to plot some kind of capacity / Gantt chart.
    /// </summary>
    public class Assignment : IWithin
    {
        public ProjectStatus ProjectStatus { get; private set; }

        public SubTask SubTask { get; private set; }

        public Assignment(SubTask subTask, ProjectStatus projectStatus)
        {
            SubTask = subTask;
            ProjectStatus = projectStatus;
        }

        public bool IsWithin(DateTime testDate)
        {
            return SubTask.IsWithin(testDate);
        }

        public bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return SubTask.IsWithin(startDate, endDate);
        }
    }
}
