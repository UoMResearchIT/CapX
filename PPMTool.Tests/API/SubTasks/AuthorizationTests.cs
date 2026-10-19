namespace PPMTool.Tests.API.SubTasks
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllSubTasksAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"subTasks/getAll?projectId={ProjectId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
