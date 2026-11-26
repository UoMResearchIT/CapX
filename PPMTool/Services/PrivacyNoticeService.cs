// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// The institution's configured in-app privacy notice.
    /// </summary>
    public sealed record PrivacyNoticeConfiguration(string Content, string Url, DateOnly Date);

    /// <summary>
    /// Loads the current privacy notice and records user acknowledgements.
    /// </summary>
    public sealed class PrivacyNoticeService
    {
        private readonly SettingsService settingsService;
        private readonly HtmlContentSanitizerService htmlContentSanitizer;
        private readonly IDbContextFactory<PPMToolContext> contextFactory;

        public PrivacyNoticeService(
            SettingsService settingsService,
            HtmlContentSanitizerService htmlContentSanitizer,
            IDbContextFactory<PPMToolContext> contextFactory)
        {
            this.settingsService = settingsService;
            this.htmlContentSanitizer = htmlContentSanitizer;
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// Gets the configured notice, or null when its content, URL, or date is incomplete or invalid.
        /// </summary>
        public PrivacyNoticeConfiguration? GetConfiguredNotice()
        {
            var content = htmlContentSanitizer.Sanitize(settingsService.GetSetting(SettingType.PrivacyNoticeContent));
            var url = settingsService.GetSetting(SettingType.PrivacyNoticeUrl).Trim();
            var dateValue = settingsService.GetSetting(SettingType.PrivacyNoticeDate).Trim();

            if (string.IsNullOrWhiteSpace(content) ||
                !IsValidPrivacyNoticeUrl(url) ||
                string.IsNullOrWhiteSpace(url) ||
                !TryParsePrivacyNoticeDate(dateValue, out var date))
            {
                return null;
            }

            return new PrivacyNoticeConfiguration(content, url, date);
        }

        /// <summary>
        /// Determines whether the current notice must be presented to the user.
        /// The date is compared for equality, not as an ordered version number.
        /// </summary>
        public static bool RequiresAcknowledgement(User user, PrivacyNoticeConfiguration notice)
        {
            return user.PrivacyNoticeAcknowledgedAt == null ||
                user.PrivacyNoticeAcknowledgedDate != notice.Date;
        }

        /// <summary>
        /// Validates an optional absolute HTTP or HTTPS privacy notice URL.
        /// </summary>
        public static bool IsValidPrivacyNoticeUrl(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        }

        /// <summary>
        /// Validates an optional ISO-8601 calendar date.
        /// </summary>
        public static bool TryParsePrivacyNoticeDate(string? value, out DateOnly date)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                date = default;
                return false;
            }

            return DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        /// <summary>
        /// Records the date shown and the time at which this user acknowledged it.
        /// </summary>
        public async Task<DateTime> AcknowledgeAsync(int userId, DateOnly noticeDate)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var user = await context.Users.SingleOrDefaultAsync(x => x.UserId == userId)
                ?? throw new InvalidOperationException($"User {userId} was not found while recording a privacy notice acknowledgement.");

            var acknowledgedAt = DateTime.Now;
            user.PrivacyNoticeAcknowledgedAt = acknowledgedAt;
            user.PrivacyNoticeAcknowledgedDate = noticeDate;
            await context.SaveChangesAsync();

            return acknowledgedAt;
        }
    }
}
