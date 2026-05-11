using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the state of the application settings.
    /// </summary>
    public class SettingsService
    {
        // The state of the settings should be cached in memory as well as the DB for performance
        private IDictionary<SettingType, string> SettingStates { get; set; } = new Dictionary<SettingType, string>();

        /// <summary>
        /// Method to initialise the cache from the database
        /// </summary>
        /// <returns></returns>
        public async Task IntialiseServiceCacheAsync(PPMToolContext context)
        {
            var settings = await GetAllSettingsAsync(context);
            SettingStates = settings.ToDictionary(x => x.SettingType, x => x.SettingValue);
        }

        /// <summary>
        /// Method to pull the full information about the features out of the database
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal async Task<List<Setting>> GetAllSettingsAsync(PPMToolContext context)
        {
            return await context.Settings.ToListAsync();
        }

        /// <summary>
        /// Updates the value of a particular setting in the local cache and the DB if commiting
        /// </summary>
        /// <param name="context"></param>
        /// <param name="setting"></param>
        /// <param name="commitChanges"></param>
        internal void UpdateSettingValue(PPMToolContext context, Setting setting, bool commitChanges = true)
        {
            SettingStates[setting.SettingType] = setting.SettingValue.Trim();
            context.Settings.Update(setting);
            if (commitChanges)
            {
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Get the value of a particular setting from the local cache
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public string GetSetting(SettingType setting)
        {
            if (SettingStates.ContainsKey(setting))
            {
                return SettingStates[setting];
            }
            return string.Empty;
        }
    }
}
