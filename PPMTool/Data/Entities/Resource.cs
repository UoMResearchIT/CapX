using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource
    {
        public int ResourceId { get; set; }

        public Person Person { get; set; }

        public double Percentage { get; set; }
    }
}
