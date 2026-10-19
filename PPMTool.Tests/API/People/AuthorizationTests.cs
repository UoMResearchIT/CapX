namespace PPMTool.Tests.API.People
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllPeopleAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("people/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
