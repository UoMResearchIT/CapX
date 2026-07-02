// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests
{
    [SetUpFixture]
    public class Setup
    {
        public static string BaseUrl { get; } = "https://localhost:5001";

        [OneTimeSetUp]
        public async Task SetupForAll()
        {
            // Wait for the server to be ready before running tests
            await WaitForServerAsync();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        /// <summary>
        /// Waits for the application server to be ready before running tests.
        /// This ensures that the server is accessible and responding to requests.
        /// </summary>
        private static async Task WaitForServerAsync(int maxRetries = 30, int delayMs = 1000)
        {
            using var handler = new HttpClientHandler();
            // Ignore SSL certificate issues for localhost testing
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            using var client = new HttpClient(handler);
            var retries = 0;

            while (retries < maxRetries)
            {
                try
                {
                    var response = await client.GetAsync($"{BaseUrl}/");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✓ Server is ready");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server not ready yet ({retries + 1}/{maxRetries}): {ex.Message}");
                }

                retries++;
                await Task.Delay(delayMs);
            }

            throw new InvalidOperationException($"Server at {BaseUrl} did not become ready after {maxRetries * delayMs}ms");
        }
    }
}
