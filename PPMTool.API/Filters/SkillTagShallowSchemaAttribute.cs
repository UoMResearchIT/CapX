// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.Attributes
{
    /// <summary>
    /// Attribute to tag a method in the API so that it invokes the appropriate operation filter during doc gen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class SkillTagShallowSchemaAttribute : Attribute
    {
    }
}
