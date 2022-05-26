using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Services
{
    public class TagService
    {
        internal IEnumerable<string> GetAllTags()
        {
            // TODO: This is mocked
            return new List<string>
            {
                "C#",
                "MATLAB",
                "GPU"
            };
        }
    }
}
