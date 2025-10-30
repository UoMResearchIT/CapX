using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the state of the application features.
    /// </summary>
    public class FeatureService
    {
        // TODO: The state of the features should be cached in memory as well as the DB for performance.
        internal List<Feature> GetAllFeatures(PPMToolContext context)
        {
            throw new NotImplementedException();
        }

        internal void UpdateFeatureState(PPMToolContext context, Feature feature)
        {
            throw new NotImplementedException();
        }
    }
}
