using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    public enum FundingStatus
    {
        /// <summary>
        /// Projects that are in preparation but the funding has not been submitted yet
        /// </summary>
        [Description("Awaiting Submission")]
        AwaitingSubmission,

        /// <summary>
        /// Grant proposals have gone in and we are awaiting the outcome
        /// </summary>
        [Description("Awaiting Outcome")]
        AwaitingOutcome,

        /// <summary>
        /// Funding has been successfully secured and project is scheduled
        /// </summary>
        Funded,

        /// <summary>
        /// Project is currently underway
        /// </summary>
        Active,

        /// <summary>
        /// Project is in maintenance phase and not under active development but is still live
        /// </summary>
        Maintenance,

        /// <summary>
        /// Project is finished and not something we are working on anymore
        /// </summary>
        Finished
    }
}
