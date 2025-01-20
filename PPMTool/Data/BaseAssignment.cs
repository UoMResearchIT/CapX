using System;
using PPMTool.Enums;

namespace PPMTool.Data
{
    public abstract class BaseAssignment : IWithin
    {
        public abstract bool IsWithin(DateTime testDate);
        public abstract bool IsWithin(DateTime startDate, DateTime endDate);

        public ProjectStatus ProjectStatus { get; private set; }

        public BaseAssignment(ProjectStatus projectStatus)
        {
            ProjectStatus = projectStatus;
        }
    }
}
