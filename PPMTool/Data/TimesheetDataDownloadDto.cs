using System;
using static PPMTool.Enums.Extensions;

namespace PPMTool.Data
{
    public class TimesheetDataDownloadDto
    {
        /// <summary>
        /// Data transfer object which holds timesheet daily task data for the download feature.
        /// </summary>
        [ExcelHeader("Contractor Name")]
        public string PersonName { get; set; }

        [ExcelHeader("Date")]
        public DateTime Date { get; set; }

        [ExcelHeader("Hours Worked")]
        public double HoursWorked { get; set; }
    }
}
