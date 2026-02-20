// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass
    {
        public virtual Person Person { get; set; }

        public abstract string GetSensibleObjectName();
    }
}
