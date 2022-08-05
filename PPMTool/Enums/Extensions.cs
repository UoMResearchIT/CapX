using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetExtensions;

namespace PPMTool.Enums
{
    public static class Extensions
    {
        public static string ToNiceString(this Enum me)
        {
            return me.GetDescription() ?? me.ToString();
        }
    }
}
