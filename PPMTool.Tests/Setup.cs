// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

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