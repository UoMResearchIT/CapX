namespace PPMTool.Tests.Core
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class PageLoadTests : PageTest
    {
        [Test]
        public async Task HomepageShouldLoadWithCorrectTitleAndNoCrashBanner()
        {
            // Navigate the browser to the homepage
            await Page.GotoAsync($"{Setup.BaseUrl}");

            // Assert that the page title is correct
            await Expect(Page).ToHaveTitleAsync("CapX - Log In");

            // Assert that the Blazor crash banner is not visible
            var crashBanner = Page.Locator("#blazor-error-ui");
            await Expect(crashBanner).Not.ToBeVisibleAsync();
        }
    }
}
