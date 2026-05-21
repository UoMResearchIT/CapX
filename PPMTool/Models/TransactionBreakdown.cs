using PPMTool.Data.Entities;

namespace PPMTool.Models
{
    /// <summary>
    /// Represents the state of funding for a project
    /// </summary>
    public class TransactionBreakdown
    {
        /// <summary>
        /// The amount of funds associated with DA-funded resources
        /// </summary>
        public double DirectlyAllocated { get; }

        /// <summary>
        /// The amount of funds associated with DI-funded resources
        /// </summary>
        public double DirectlyIncurred { get; }

        /// <summary>
        /// The amound of funds requested from invoices sent to customers
        /// </summary>
        public double Invoices { get; }

        /// <summary>
        /// The amount of funds received from payments made to us
        /// </summary>
        public double Payments { get; }

        /// <summary>
        /// Funding sources used to compute the values
        /// </summary>
        public IEnumerable<FundingSource> FundingSources { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="directlyAllocated"></param>
        /// <param name="directlyIncurred"></param>
        /// <param name="invoices"></param>
        /// <param name="payments"></param>
        /// <param name="sources"></param>
        public TransactionBreakdown(
            double directlyAllocated,
            double directlyIncurred,
            double invoices,
            double payments,
            IEnumerable<FundingSource> sources)
        {
            DirectlyAllocated = directlyAllocated;
            DirectlyIncurred = directlyIncurred;
            Invoices = invoices;
            Payments = payments;
            FundingSources = sources;
        }
    }
}
