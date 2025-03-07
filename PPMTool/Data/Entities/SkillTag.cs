// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.Collections.Generic;

namespace PPMTool.Data.Entities
{
    public class SkillTag : ILoggableClass
    {
        public int SkillTagId { get; set; }

        public string Name { get; set; }

        public ICollection<Person> People { get; set; }

        public string GetSensibleObjectName()
        {
            return $"Skill Tag: {Name}";
        }
    }
}
