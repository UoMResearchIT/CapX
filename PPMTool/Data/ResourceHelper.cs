using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PPMTool.Data
{
    public class ResourceHelper
    {
        internal static IEnumerable<string> AvailableInnateActivities { get; private set; }

        /// <summary>
        /// Initialised the helper lists by reading the resource files
        /// </summary>
        internal static void Initialise()
        {
            // Parse all the strings from the resource file
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly?.GetManifestResourceStream("PPMTool.InnateActivityList.csv"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    if (stream != null)
                    {
                        var items = new List<string>();
                        while (!reader.EndOfStream)
                        {
                            var values = reader.ReadLine().Split('|');
                            items.Add(string.Join(" - ", values));
                            AvailableInnateActivities = items;
                        }
                    }
                    else
                    {
                        throw new IOException("Could not load resource file!");
                    }
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Gets the default Innate Activity or "None" if there are no activities
        /// </summary>
        /// <returns></returns>
        internal static string GetDefaultInnateActivity()
        {
            // Default is considered the first in the list
            return AvailableInnateActivities.FirstOrDefault() ?? "None";
        }
    }
}
