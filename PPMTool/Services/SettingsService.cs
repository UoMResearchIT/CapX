using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the state of the application settings.
    /// </summary>
    public class SettingsService : BaseEntityService<Setting>
    {
        // The state of the settings should be cached in memory as well as the DB for performance
        private IDictionary<SettingType, string> SettingStates { get; set; } = new Dictionary<SettingType, string>();

        private async Task SetDefaultSettings(PPMToolContext context)
        {
            // Clear the table and re-initialise
            context.Settings.RemoveRange(context.Settings);

            // List of default settings for each of the setting types
            var allSettingTypes = Enum.GetValues<SettingType>().ToList();
            var defaultSettings = allSettingTypes.Select(setting => new Setting
            {
                SettingType = setting,
                SettingValue = setting.GetDefaultSettingValue(),
                Description = setting.GetDescription()
            }).ToList();
            context.Settings.AddRange(defaultSettings);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Method to initialise the cache from the database
        /// </summary>
        /// <returns></returns>
        public async Task IntialiseServiceCacheAsync(PPMToolContext context)
        {
            // If we have no settings in the DB then we need to set the defaults before populating the cache
            if (!context.Settings.Any())
            {
                await SetDefaultSettings(context);
            }

            // Pull the settings out of the DB and populate the cache
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

        /// <summary>
        /// Override for the add method. Not implemented as settings don't work like this.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int Add(PPMToolContext context, Setting entity, bool commitChanges = true)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IEnumerable<Setting> GetAll(PPMToolContext context)
        {
            return GetAllSettingsAsync(context).Result;
        }

        /// <summary>
        /// Updates the specified setting in the database and optionally commits the changes.
        /// </summary>
        /// <remarks>If commitChanges is set to false, the changes will not be persisted to the database
        /// until SaveChanges is called on the context.</remarks>
        /// <param name="context">The database context used to access and update the setting.</param>
        /// <param name="entity">The setting entity containing the updated values to be saved.</param>
        /// <param name="commitChanges">true to commit the changes to the database immediately; otherwise, false.</param>
        /// <returns>The unique identifier of the updated setting.</returns>
        public override int Update(PPMToolContext context, Setting entity, bool commitChanges = true)
        {
            // Update the local cache and the DB
            UpdateSettingValue(context, entity, commitChanges);
            return entity.SettingId;
        }

        /// <summary>
        /// Override for the delete method. Not implemented as settings don't work like this.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void Delete(PPMToolContext context, Setting entity, bool commitChanges = true)
        {
            throw new NotImplementedException();
        }
    }
}
