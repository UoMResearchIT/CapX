using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    public class Tag
    {
        public string Name { get; }

        public bool Checked { get; set; }

        public Tag(string name)
        {
            Name = name;
        }
    }
}
