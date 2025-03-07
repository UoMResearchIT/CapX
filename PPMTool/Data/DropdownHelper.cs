// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.Collections.Generic;
using PPMTool.Enums;

namespace PPMTool.Data
{
    public abstract class DropdownHelper
    {
        public static IEnumerable<School> GetSchoolsForFaculty(Faculty faculty)
        {
            switch (faculty)
            {
                case Faculty.FSE:
                    return new List<School> { School.SE, School.SNS };
                case Faculty.FBMH:
                    return new List<School> { School.SBS, School.SMS, School.SHS };
                case Faculty.FHUMS:
                    return new List<School> { School.AMBS, School.SALC, School.SEED, School.SSS };
                default:
                    return new List<School> { School.None };
            }
        }
    }
}
