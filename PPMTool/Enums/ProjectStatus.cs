using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    /// <summary>
    /// These are just broad scoped statuses of a project
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>
        /// Projects that are in preparation and have not had any confirmed funding
        /// </summary>
        Unfunded,

        /// <summary>
        /// Projects that have had their funding confirmed and are going ahead but not yet started
        /// </summary>
        Funded,

        /// <summary>
        /// Project is currently underway
        /// </summary>
        Active,

        /// <summary>
        /// Projects that have been started but are currently paused for whatever reason
        /// </summary>
        Paused,

        /// <summary>
        /// Project is in maintenance phase and not under active development but is still live
        /// </summary>
        Maintenance,

        /// <summary>
        /// Project is finished and not something we are working on anymore
        /// </summary>
        Finished,

        /// <summary>
        /// Project is cancelled, bid failed or we weren't able to resource it
        /// </summary>
        Cancelled
    }
}
