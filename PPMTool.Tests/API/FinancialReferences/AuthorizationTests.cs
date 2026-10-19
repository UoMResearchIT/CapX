namespace PPMTool.Tests.API.FinancialReferences
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllFinancialReferencesAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("financialReferences/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
