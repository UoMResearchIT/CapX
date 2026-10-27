namespace PPMTool.Tests.API.FundingSources
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllFundingSourcesAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"fundingSources/getAll?projectId={ProjectId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
