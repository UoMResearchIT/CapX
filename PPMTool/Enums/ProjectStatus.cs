using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Statuses a project can be in as part of our demand and monitoring process
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>
        /// A new request that is being discussed and written up and has not had its scope confirmed by the customer yet
        /// </summary>
        [Description("New Request")]
        NewRequest,

        /// <summary>
        /// Projects that have been agreed and are awaiting submission to a funder
        /// </summary>
        [Description("Awaiting Submission")]
        AwaitingSubmission,

        /// <summary>
        /// Projects that have funding bids submitted but no outcome yet
        /// </summary>
        [Description("Awaiting Outcome")]
        AwaitingOutcome,

        /// <summary>
        /// Projects that have had their funding confirmed and are going ahead but not yet started
        /// </summary>
        Funded,

        /// <summary>
        /// Project is currently underway
        /// </summary>
        Active,

        /// <summary>
        /// Projects that have been previously started but are currently paused for whatever reason
        /// </summary>
        Paused,

        /// <summary>
        /// Project is in maintenance phase and not under active development but still requires RSE effort
        /// </summary>
        Maintenance,

        /// <summary>
        /// Project is finished and not something we are working on anymore
        /// </summary>
        Finished,

        /// <summary>
        /// Project cancelled by customer and never started
        /// </summary>
        [Description("Cancelled by Customer")]
        CancelledByCustomer,

        /// <summary>
        /// Project cancelled due to funding failure and never started
        /// </summary>
        [Description("Bid Failed")]
        CancelledBidFailed,

        /// <summary>
        /// Project cancelled because we couldn't resource it and never started
        /// </summary>
        [Description("Unable to Resource")]
        CancelledNoResource
    }

    static class ProjectStatusExtensions
    {
        /// <summary>
        /// Project status is one of the cancelled states or finished
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsFinishedOrCancelled(this ProjectStatus status)
        {
            return
                status == ProjectStatus.Finished ||
                status == ProjectStatus.CancelledByCustomer ||
                status == ProjectStatus.CancelledBidFailed ||
                status == ProjectStatus.CancelledNoResource;
        }

        /// <summary>
        /// Project status is one of the cancelled states
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsCancelled(this ProjectStatus status)
        {
            return
                status == ProjectStatus.CancelledByCustomer ||
                status == ProjectStatus.CancelledBidFailed ||
                status == ProjectStatus.CancelledNoResource;
        }

        /// <summary>
        /// Project status is unfunded or cancelled
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsUnconfirmed(this ProjectStatus status)
        {
            return
                status.IsCancelled() ||
                status.IsUnfunded();
        }

        /// <summary>
        /// Project status is one of the pre-funded statuses
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsUnfunded(this ProjectStatus status)
        {
            return
                status == ProjectStatus.NewRequest ||
                status == ProjectStatus.AwaitingSubmission ||
                status == ProjectStatus.AwaitingOutcome;
        }
    }
}
