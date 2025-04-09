using System.Collections.Generic;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Class to represent a window when someone has availability
    /// </summary>
    public class CapacityQueryItem
    {
        /// <summary>
        /// Person associated with this query result
        /// </summary>
        public Person Person { get; }

        /// <summary>
        /// The list of blocks that represent this person's availability
        /// </summary>
        public IEnumerable<ChartItem> Blocks { get; }

        /// <summary>
        /// The total unmet days summed over all the blocks after assignment
        /// </summary>
        public double UnmetDemand { get; }

        /// <summary>
        /// The total unmet days after assignment disregarding provisional assignments
        /// </summary>
        public double UnmetDemandNoProvisional { get; }

        /// <summary>
        /// Number of skills not matched
        /// </summary>
        public int UnmatchedSkills { get; }

        /// <summary>
        /// Whether any skills matched are marked to develop
        /// </summary>
        public bool IsToDevelop { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="person"></param>
        /// <param name="blocks"></param>
        /// <param name="skillToDevelop"></param>
        /// <param name="unmatchedSkills"></param>
        /// <param name="unmetArea"></param>
        /// <param name="unmetAreaNoProvisional"></param>
        public CapacityQueryItem(
            Person person,
            IEnumerable<ChartItem> blocks,
            double unmetArea,
            double unmetAreaNoProvisional,
            int unmatchedSkills,
            bool skillToDevelop)
        {
            Person = person;
            Blocks = blocks;
            UnmetDemand = unmetArea;
            UnmetDemandNoProvisional = unmetAreaNoProvisional;
            UnmatchedSkills = unmatchedSkills;
            IsToDevelop = skillToDevelop;
        }
    }
}
