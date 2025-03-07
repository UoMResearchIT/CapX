// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass
    {
        public Person Person { get; set; }

        public abstract string GetSensibleObjectName();
    }
}
