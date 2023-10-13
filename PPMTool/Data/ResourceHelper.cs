using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Firebase.Storage;

namespace PPMTool.Data
{
    public class ResourceHelper
    {
        internal static IEnumerable<string> AvailableInnateActivities { get; private set; }

        private static string storageUrl = "capx-9cae0.appspot.com";
        private static string filename = "InnateActivityList.csv";

        /// <summary>
        /// Initialised the helper lists by reading the resource files
        /// </summary>
        internal static void Initialise()
        {
            // Pull file from Firebase
            Task.Run(async () =>
            {
                try
                {
                    var firebaseStorage = new FirebaseStorage(storageUrl);
                    var filePath = await firebaseStorage
                        .Child(filename)
                        .GetDownloadUrlAsync();
                    HttpClient client = new HttpClient();
                    var serialised = await client.GetStringAsync(filePath);

                    // Reformat
                    List<string> temp = serialised.Split(Environment.NewLine).ToList();
                    List<string> items = new List<string>();
                    foreach (var s in temp)
                    {
                        var values = s.Split('|');
                        items.Add(string.Join(" - ", values));
                        AvailableInnateActivities = items;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }

        /// <summary>
        /// Returns the word "None"
        /// </summary>
        /// <returns></returns>
        internal static string GetDefaultInnateActivity()
        {
            // Default is just "none"
            return "None";
        }
    }
}
