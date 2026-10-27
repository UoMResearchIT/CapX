namespace PPMTool.Tests.API.Projects
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllProjectsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("projects/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
