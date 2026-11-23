// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Enums;

namespace PPMTool.Tests.Core
{
    [TestFixture]
    public class SettingTypeTests
    {
        /// <summary>
        /// <see cref="SettingType"/> is stored as its number, and C# accepts two members
        /// with the same explicit value, so a merge that numbers two settings alike
        /// would otherwise compile.
        /// </summary>
        [Test]
        public void SettingTypeValuesMustBeUnique()
        {
            var values = Enum.GetValues<SettingType>().Select(x => (int)x).ToList();
            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Count));
        }
    }
}
