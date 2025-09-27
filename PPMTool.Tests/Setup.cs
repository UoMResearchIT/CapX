namespace PPMTool.Tests
{
    [SetUpFixture]
    public class Setup
    {
        public static string BaseUrl { get; } = "https://localhost:5001";

        [OneTimeSetUp]
        public void SetupForAll()
        {
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }
    }
}