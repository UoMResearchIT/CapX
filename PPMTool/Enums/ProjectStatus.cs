using System.ComponentModel;

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
        /// Project cancelled by customer
        /// </summary>
        [Description("Cancelled by Customer")]
        CancelledByCustomer,

        /// <summary>
        /// project cancelled due to funding failure
        /// </summary>
        [Description("Bid Failed")]
        CancelledBidFailed,

        /// <summary>
        /// Project cancelled because we couldn't resource it
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
                status == ProjectStatus.Unfunded;
        }
    }
}
