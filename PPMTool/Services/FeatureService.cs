using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the state of the application features.
    /// </summary>
    public class FeatureService
    {
        // The state of the features should be cached in memory as well as the DB for performance
        private IDictionary<FeatureType, bool> FeatureState { get; set; } = new Dictionary<FeatureType, bool>();

        /// <summary>
        /// Method to initialise the cache from the database
        /// </summary>
        /// <returns></returns>
        public async Task IntialiseServiceCacheAsync(PPMToolContext context)
        {
            var features = await GetAllFeaturesAsync(context);
            FeatureState = features.ToDictionary(f => f.FeatureType, f => f.Enabled);
        }

        /// <summary>
        /// Method to pull the full information about the features out of the database
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal async Task<List<Feature>> GetAllFeaturesAsync(PPMToolContext context)
        {
            return await context.Features.ToListAsync();
        }

        /// <summary>
        /// Updates the state of a particular feature in the local cache and the DB if commiting
        /// </summary>
        /// <param name="context"></param>
        /// <param name="feature"></param>
        /// <param name="commitChanges"></param>
        internal void UpdateFeatureState(PPMToolContext context, Feature feature, bool commitChanges = true)
        {
            FeatureState[feature.FeatureType] = feature.Enabled;
            context.Features.Update(feature);
            if (commitChanges)
            {
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Check whether a feature is enabled
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public bool IsFeatureEnabled(FeatureType feature)
        {
            if (FeatureState.ContainsKey(feature))
            {
                return FeatureState[feature];
            }
            return false;
        }
    }
}
