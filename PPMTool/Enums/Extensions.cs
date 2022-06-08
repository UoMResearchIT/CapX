using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetExtensions;

namespace PPMTool.Enums
{
    public static class Extensions
    {
        public static string ToNiceString(this FundingStatus me)
        {
            return me.GetDescription() ?? me.ToString();
        }

        public static string ToNiceString(this Portfolio me)
        {
            return me.GetDescription() ?? me.ToString();
        }

        public static string ToNiceString(this TaskType me)
        {
            return me.GetDescription() ?? me.ToString();
        }
    }
}
