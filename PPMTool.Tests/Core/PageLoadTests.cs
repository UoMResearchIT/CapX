// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.Core
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class PageLoadTests : PageTest
    {
        private const int pageLoadTimeoutMs = 5000;

        private async Task VerifyPageLoaded(string url, string expectedTitle)
        {
            // Navigate to the page
            await Page.GotoAsync($"{Setup.BaseUrl}{url}");

            // Assert that the page title is correct with retry timeout
            await Expect(Page).ToHaveTitleAsync(expectedTitle, new() { Timeout = pageLoadTimeoutMs });

            // Assert that the Blazor crash banner is not visible with retry timeout
            var crashBanner = Page.Locator("#blazor-error-ui");
            await Expect(crashBanner).ToBeHiddenAsync(new() { Timeout = pageLoadTimeoutMs });
        }

        [Test]
        public async Task HomepageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/", "CapX - Log In");
        }

        [Test]
        public async Task MyProjectsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/myprojects", "My Projects - CapX");
        }

        [Test]
        public async Task ProjectsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/projects", "Projects - CapX");
        }

        [Test]
        public async Task PeoplePageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/people", "People - CapX");
        }

        [Test]
        public async Task CapacityPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/capacity", "Capacity - CapX");
        }

        [Test]
        public async Task TimesheetsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/timesheets", "Timesheets - CapX");
        }

        [Test]
        public async Task AbsencesPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/absences", "Absences - CapX");
        }

        [Test]
        public async Task CompetencyFrameworkPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/competencies", "Development Journey - CapX");
        }

        [Test]
        public async Task ManageSkillsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/manageskills", "Manage Skills - CapX");
        }

        [Test]
        public async Task ManageAccessPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/manageaccess", "Manage Access - CapX");
        }

        [Test]
        public async Task ManageSettingsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managesettings", "Manage Settings - CapX");
        }

        [Test]
        public async Task ManageFeaturesPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managefeatures", "Manage Features - CapX");
        }

        [Test]
        public async Task ManageOrgUnitsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/manageorgunits", "Manage Org Units - CapX");
        }

        [Test]
        public async Task ManageInnateCodesPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managecodes", "Manage Timesheet Codes - CapX");
        }

        [Test]
        public async Task ManageFinancialItemsPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managefinancialitems", "Manage Finance Items - CapX");
        }

        [Test]
        public async Task ManageFinancialReferencesPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managefinref", "Manage Financial Refs - CapX");
        }

        [Test]
        public async Task FinanceSummaryPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managefinancialitems/summary", "Finance Summary - CapX");
        }

        [Test]
        public async Task DataDashboardPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/datadashboard", "Data Dashboard - CapX");
        }

        [Test]
        public async Task EstimateCostPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/estimatecost", "Estimate Cost - CapX");
        }

        [Test]
        public async Task ManagementCapacityPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/managementcapacity", "Management Capacity - CapX");
        }

        [Test]
        public async Task WorkloadModelAnalysisPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/wlmanalysis", "WLM Analysis - CapX");
        }

        [Test]
        public async Task ProjectBulletinBoardPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/projectbulletinboard", "Available Projects - CapX");
        }

        [Test]
        public async Task UserProfilePageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/profile", "Profile - CapX");
        }

        [Test]
        public async Task NothingHerePageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/nothinghere", "Nothing Here - CapX");
        }
    }
}
