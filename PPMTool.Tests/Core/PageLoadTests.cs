// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.Core
{
    [Parallelizable(ParallelScope.None)]
    [TestFixture]
    public class PageLoadTests : PageTest
    {
        // Timeouts (in milliseconds)
        private const int pageLoadTimeoutMs = 5000;
        private const int pageTitleTimeoutMs = 2000;
        private const int loginButtonTimeoutMs = 2000;
        private const int tearDownDelayMs = 1000;
        private const int pageStabilizationDelayMs = 1000;
        private const int loginAuthenticationDelayMs = 1000;
        private const int loginRetryDelayMs = 1000;

        // Retry configuration
        private const int navigationRetries = 5;
        private const int retryDelayMs = 1000;
        private const int maxLoginAttempts = 3;

        [TearDown]
        public async Task TearDownTest()
        {
            // Clear cookies and storage to ensure clean state between tests
            // This forces re-authentication each time
            try
            {
                await Page.Context.ClearCookiesAsync();
                await Page.EvaluateAsync("() => localStorage.clear()");
                await Page.EvaluateAsync("() => sessionStorage.clear()");
            }
            catch
            {
                // Ignore errors during cleanup
            }

            // Add a delay between tests to allow the server to recover
            await Task.Delay(tearDownDelayMs);
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

            // Check if we're on the login page (unauthenticated) by checking the page title
            if (!skipLoginCheck)
            {
                // Wait a bit for the page to stabilize
                await Task.Delay(pageStabilizationDelayMs);

                // Wait for the page to have a title using timeout via context options
                var titleRegex = new Regex(".*");
                await Expect(Page).ToHaveTitleAsync(titleRegex);

                var currentTitle = await Page.TitleAsync();
                if (currentTitle == "CapX - Log In")
                {
                    // We're on the login page, need to authenticate
                    Console.WriteLine($"Detected login page, authenticating before navigating to {url}");
                    await HandleLoginAndNavigateAsync(url);
                }
                else
                {
                    // Verify we have the content we expect by waiting for page to fully load
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }

            // Assert that the page title is correct
            await Expect(Page).ToHaveTitleAsync(expectedTitle);

            // Assert that the Blazor crash banner is not visible
            var crashBanner = Page.Locator("#blazor-error-ui");
            await Expect(crashBanner).ToBeHiddenAsync();
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
        /// Handles the login process by clicking the Log In button and waiting for authentication to complete,
        /// then navigates to the target URL. Includes retry logic to handle transient failures.
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <returns></returns>
        private async Task HandleLoginAndNavigateAsync(string targetUrl)
        {
            int loginAttempts = 0;

            while (loginAttempts < maxLoginAttempts)
            {
                try
                {
                    // Find the Log in button - in LOCAL mode this link contains the username parameter
                    var loginButton = Page.Locator("a:has-text('Log in')").First;

                    if (await loginButton.IsVisibleAsync())
                    {
                        Console.WriteLine($"Login attempt {loginAttempts + 1}/{maxLoginAttempts}: Clicking login button");

                        // Click the Log in button
                        await loginButton.ClickAsync();

                        // Wait for navigation to complete
                        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                        // Small delay to let authentication finish
                        await Task.Delay(loginAuthenticationDelayMs);

                        // Navigate to the target page
                        await NavigateWithRetryAsync($"{Setup.BaseUrl}{targetUrl}");

                        // Verify we're not on the login page anymore
                        var titleAfterLogin = await Page.TitleAsync();
                        if (titleAfterLogin != "CapX - Log In")
                        {
                            Console.WriteLine($"Successfully authenticated and navigated to {targetUrl}");
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException("Still on login page after clicking login button");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Login button not visible on login page");
                    }
                }
                catch (Exception ex)
                {
                    loginAttempts++;
                    if (loginAttempts >= maxLoginAttempts)
                    {
                        throw new InvalidOperationException($"Failed to authenticate after {maxLoginAttempts} attempts: {ex.Message}", ex);
                    }

                    Console.WriteLine($"Login attempt failed: {ex.Message}. Retrying...");
                    await Task.Delay(loginRetryDelayMs);
                }
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
    }
}
