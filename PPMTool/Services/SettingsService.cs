using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using Radzen;

namespace PPMTool.Services
{
    /// <summary>
    /// Service to manage the state of the application settings.
    /// </summary>
    public class SettingsService : BaseEntityService<Setting>
    {
        // The state of the settings should be cached in memory as well as the DB for performance
        private IDictionary<SettingType, string> SettingStates { get; set; } = new Dictionary<SettingType, string>();

        /// <summary>
        /// Resets the settings table in the specified context to contain the default settings for all setting types.
        /// </summary>
        /// <remarks>This method removes all existing settings and repopulates the table with default
        /// values for each setting type. Changes are saved asynchronously to the database.</remarks>
        /// <param name="context">The database context in which to reset and initialize the settings table. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
        /// Method to initialise the cache from the database.
        /// If there are no settings in the database then it will set the defaults and then populate the cache.
        /// It also checks that the settings in the DB are in sync with the enum and adds any new ones or removes any old ones as necessary.
        /// </summary>
        /// <param name="context">The database context to use for initialising the cache. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task IntialiseServiceCacheAsync(PPMToolContext context)
        {
            // If we have no settings in the DB then we need to set the defaults before populating the cache
            if (!context.Settings.Any())
            {
                await SetDefaultSettings(context);
            }

            // Resync the DB with the enum in case of any changes
            var allSettingTypes = Enum.GetValues<SettingType>().ToList();
            foreach (var setting in allSettingTypes)
            {
                // New setting added to the enum - add to the DB with the default value and description
                if (!context.Settings.Any(s => s.SettingType == setting))
                {
                    context.Settings.Add(new Setting
                    {
                        SettingType = setting,
                        SettingValue = setting.GetDefaultSettingValue(),
                        Description = setting.GetDescription()
                    });
                    await context.SaveChangesAsync();
                }

                // Setting removed from the enum - remove from the DB
                else if (!allSettingTypes.Contains(setting))
                {
                    context.Settings.RemoveRange(context.Settings.Where(s => s.SettingType == setting));
                    await context.SaveChangesAsync();
                }
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
        internal async void UpdateSettingValue(PPMToolContext context, Setting setting, bool commitChanges = true)
        {
            // Strip the whitespace
            setting.SettingValue = setting.SettingValue.Trim();

            // Set in the cache
            SettingStates[setting.SettingType] = setting.SettingValue;

            // Update the DB
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
        /// Retrieves the value of the specified setting, converted to the specified type. Returns a default value if
        /// the setting is not found or cannot be converted.
        /// </summary>
        /// <remarks>If the setting value cannot be converted to the specified type, the method returns
        /// the default value for type T. This method is useful for retrieving strongly typed configuration values with
        /// a fallback.</remarks>
        /// <typeparam name="T">The type to which the setting value is converted and returned.</typeparam>
        /// <param name="setting">The setting to retrieve.</param>
        /// <param name="defaultValue">The value to return if the setting is not found or cannot be converted to the specified type.</param>
        /// <returns>The value of the specified setting converted to type T, or the provided default value if the setting is not
        /// found or conversion fails.</returns>
        public T GetSetting<T>(SettingType setting, T defaultValue)
        {
            string settingValue = GetSetting(setting);

            if (string.IsNullOrWhiteSpace(settingValue))
                return defaultValue;

            try
            {
                var targetType = typeof(T);

                // Handle Nullable<T>
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // Strings
                if (underlyingType == typeof(string))
                    return (T)(object)settingValue;

                // Int
                if (underlyingType == typeof(int) &&
                    int.TryParse(settingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                    return (T)(object)i;

                // Float
                if (underlyingType == typeof(float) &&
                    float.TryParse(settingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                    return (T)(object)f;

                // Double
                if (underlyingType == typeof(double) &&
                    double.TryParse(settingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return (T)(object)d;

                // Decimal
                if (underlyingType == typeof(decimal) &&
                    decimal.TryParse(settingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var m))
                    return (T)(object)m;

                // Bool
                if (underlyingType == typeof(bool) &&
                    bool.TryParse(settingValue, out var b))
                    return (T)(object)b;

                // Enum
                if (underlyingType.IsEnum &&
                    Enum.TryParse(underlyingType, settingValue, ignoreCase: true, out var e))
                    return (T)e;

                // Fallback
                return (T)Convert.ChangeType(settingValue, underlyingType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
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
