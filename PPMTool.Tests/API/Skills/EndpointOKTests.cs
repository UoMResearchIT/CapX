namespace PPMTool.Tests.API.Skills
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAllSkillsShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("/skills/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}