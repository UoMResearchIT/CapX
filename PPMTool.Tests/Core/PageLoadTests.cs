// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.Core
{
    [Parallelizable(ParallelScope.None)]
    [TestFixture]
    public class PageLoadTests : PageTest
    {
        private const int pageLoadTimeoutMs = 5000;
        private const int navigationRetries = 3;
        private const int retryDelayMs = 500;

        [TearDown]
        public async Task TearDownTest()
        {
            // Add a small delay between tests to allow the server to recover
            await Task.Delay(500);
        }

        /// <summary>
        /// Verifies that a page loads correctly by checking the title and ensuring that the Blazor crash banner is not visible.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="expectedTitle"></param>
        /// <param name="skipLoginCheck"></param>
        /// <returns></returns>
        private async Task VerifyPageLoaded(string url, string expectedTitle, bool skipLoginCheck = false)
        {
            // Navigate to the page with retry logic
            await NavigateWithRetryAsync($"{Setup.BaseUrl}{url}");

            // Check if we're on the login page (unauthenticated)
            if (!skipLoginCheck)
            {
                await HandleLoginIfNeeded();

                // Navigate to the target page (in case we were redirected)
                await NavigateWithRetryAsync($"{Setup.BaseUrl}{url}");
            }

            // Assert that the page title is correct with retry timeout
            await Expect(Page).ToHaveTitleAsync(expectedTitle, new() { Timeout = pageLoadTimeoutMs });

            // Assert that the Blazor crash banner is not visible with retry timeout
            var crashBanner = Page.Locator("#blazor-error-ui");
            await Expect(crashBanner).ToBeHiddenAsync(new() { Timeout = pageLoadTimeoutMs });
        }

        /// <summary>
        /// Navigates to a URL with retry logic to handle temporary connection issues.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task NavigateWithRetryAsync(string url)
        {
            int retries = 0;
            while (retries < navigationRetries)
            {
                try
                {
                    await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
                    return;
                }
                catch (Exception ex) when (retries < navigationRetries - 1 && 
                    (ex.Message.Contains("ERR_CONNECTION_REFUSED") || 
                     ex.Message.Contains("ERR_ABORTED") || 
                     ex.Message.Contains("ERR_NETWORK_CHANGED")))
                {
                    retries++;
                    await Task.Delay(retryDelayMs);
                }
            }

            // Final attempt without retry
            await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        }

        /// <summary>
        /// Handles the login process if the user is not authenticated. It checks for the presence of the "Log in" button, clicks it, and then clicks the auto-login link to authenticate the user.
        /// </summary>
        /// <returns></returns>
        private async Task HandleLoginIfNeeded()
        {
            // Check if the Log in button is visible (indicates we're not authenticated)
            var loginButton = Page.Locator("a:has-text('Log in')");

            try
            {
                // Only if the login button is visible should we attempt to log in
                if (await loginButton.IsVisibleAsync())
                {
                    // Click the Log in button - in LOCAL mode this link already contains the username parameter
                    await loginButton.ClickAsync();

                    // Wait for the login/redirect to complete
                    await Page.WaitForLoadStateAsync();
                }
            }
            catch
            {
                // If the login button is not found or times out, we're likely already authenticated
                // Continue without logging in
            }
        }

        [Test]
        public async Task LogInPageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            await VerifyPageLoaded("/", "CapX - Log In", true);
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
