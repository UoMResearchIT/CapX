// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.Tests.Core
{
    [TestFixture]
    public class PrivacyNoticeServiceTests
    {
        [TestCase("2026-11-26", true)]
        [TestCase("2026-02-29", false)]
        [TestCase("26/11/2026", false)]
        [TestCase("", false)]
        public void TryParsePrivacyNoticeDateAcceptsOnlyIsoCalendarDates(string value, bool expected)
        {
            var parsed = PrivacyNoticeService.TryParsePrivacyNoticeDate(value, out var date);

            Assert.That(parsed, Is.EqualTo(expected));
            if (expected)
            {
                Assert.That(date, Is.EqualTo(new DateOnly(2026, 11, 26)));
            }
        }

        [TestCase("https://example.org/privacy", true)]
        [TestCase("http://intranet.example.org/privacy", true)]
        [TestCase("/privacy", false)]
        [TestCase("javascript:alert(1)", false)]
        [TestCase("", true)]
        public void IsValidPrivacyNoticeUrlOnlyAcceptsAbsoluteHttpUrlsOrBlank(string value, bool expected)
        {
            Assert.That(PrivacyNoticeService.IsValidPrivacyNoticeUrl(value), Is.EqualTo(expected));
        }

        [Test]
        public void RequiresAcknowledgementUsesDateEquality()
        {
            var notice = new PrivacyNoticeConfiguration(
                "<p>Notice</p>",
                "https://example.org/privacy",
                new DateOnly(2026, 11, 26));
            var user = new User();

            Assert.That(PrivacyNoticeService.RequiresAcknowledgement(user, notice), Is.True);

            user.PrivacyNoticeAcknowledgedAt = new DateTime(2026, 11, 26, 10, 0, 0);
            user.PrivacyNoticeAcknowledgedDate = new DateOnly(2026, 11, 25);
            Assert.That(PrivacyNoticeService.RequiresAcknowledgement(user, notice), Is.True);

            user.PrivacyNoticeAcknowledgedDate = notice.Date;
            Assert.That(PrivacyNoticeService.RequiresAcknowledgement(user, notice), Is.False);
        }

        [Test]
        public async Task GetConfiguredNoticeReturnsNullWhenSettingsAreIncomplete()
        {
            var options = CreateOptions();
            var settingsService = new SettingsService(NullLogger<SettingsService>.Instance);
            await using (var context = new PPMToolContext(options))
            {
                await settingsService.IntialiseServiceCacheAsync(context);
            }

            var service = CreateService(settingsService, options);

            Assert.That(service.GetConfiguredNotice(), Is.Null);
        }

        [Test]
        public async Task GetConfiguredNoticeSanitizesContentAndParsesConfiguredDate()
        {
            var options = CreateOptions();
            var settingsService = new SettingsService(NullLogger<SettingsService>.Instance);
            await using (var context = new PPMToolContext(options))
            {
                context.Settings.AddRange(
                    new Setting { SettingType = SettingType.PrivacyNoticeContent, SettingValue = "<p>Notice</p><script>alert(1)</script>" },
                    new Setting { SettingType = SettingType.PrivacyNoticeUrl, SettingValue = "https://example.org/privacy" },
                    new Setting { SettingType = SettingType.PrivacyNoticeDate, SettingValue = "2026-11-26" });
                await context.SaveChangesAsync();
                await settingsService.IntialiseServiceCacheAsync(context);
            }

            var service = CreateService(settingsService, options);
            var notice = service.GetConfiguredNotice();

            Assert.That(notice, Is.Not.Null);
            Assert.That(notice!.Content, Does.Contain("<p>Notice</p>"));
            Assert.That(notice.Content, Does.Not.Contain("<script>"));
            Assert.That(notice.Url, Is.EqualTo("https://example.org/privacy"));
            Assert.That(notice.Date, Is.EqualTo(new DateOnly(2026, 11, 26)));
        }

        [TestCase("javascript:alert(1)", "2026-11-26")]
        [TestCase("https://example.org/privacy", "26/11/2026")]
        public async Task GetConfiguredNoticeReturnsNullForInvalidUrlOrDate(string url, string date)
        {
            var options = CreateOptions();
            var settingsService = new SettingsService(NullLogger<SettingsService>.Instance);
            await using (var context = new PPMToolContext(options))
            {
                context.Settings.AddRange(
                    new Setting { SettingType = SettingType.PrivacyNoticeContent, SettingValue = "<p>Notice</p>" },
                    new Setting { SettingType = SettingType.PrivacyNoticeUrl, SettingValue = url },
                    new Setting { SettingType = SettingType.PrivacyNoticeDate, SettingValue = date });
                await context.SaveChangesAsync();
                await settingsService.IntialiseServiceCacheAsync(context);
            }

            var service = CreateService(settingsService, options);

            Assert.That(service.GetConfiguredNotice(), Is.Null);
        }

        [Test]
        public async Task AcknowledgeAsyncPersistsTimestampAndNoticeDate()
        {
            var options = CreateOptions();
            int userId;
            await using (var context = new PPMToolContext(options))
            {
                var user = new User
                {
                    RoleType = RoleType.Reader,
                    CASUserName = "privacy-test",
                    Name = "Privacy Test",
                    EmailAddress = "privacy-test@example.org"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
                userId = user.UserId;
            }

            var settingsService = new SettingsService(NullLogger<SettingsService>.Instance);
            var service = CreateService(settingsService, options);
            var noticeDate = new DateOnly(2026, 11, 26);
            var acknowledgedAt = await service.AcknowledgeAsync(userId, noticeDate);

            await using var verificationContext = new PPMToolContext(options);
            var savedUser = await verificationContext.Users.SingleAsync(x => x.UserId == userId);
            Assert.That(savedUser.PrivacyNoticeAcknowledgedAt, Is.EqualTo(acknowledgedAt));
            Assert.That(savedUser.PrivacyNoticeAcknowledgedDate, Is.EqualTo(noticeDate));
        }

        private static DbContextOptions<PPMToolContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<PPMToolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static PrivacyNoticeService CreateService(
            SettingsService settingsService,
            DbContextOptions<PPMToolContext> options)
        {
            return new PrivacyNoticeService(
                settingsService,
                new HtmlContentSanitizerService(),
                new InMemoryContextFactory(options));
        }

        private sealed class InMemoryContextFactory(DbContextOptions<PPMToolContext> options)
            : IDbContextFactory<PPMToolContext>
        {
            public PPMToolContext CreateDbContext() => new(options);

            public Task<PPMToolContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(CreateDbContext());
        }
    }
}
