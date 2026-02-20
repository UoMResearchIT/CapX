// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;

namespace PPMTool.Services
{
    /// <summary>
    /// Class to encapsulate a change to an entity with values represented as strings
    /// </summary>
    public class EntityDiff<T> where T : class
    {
        public T Entity { get; }
        public EntityState State { get; }
        public string PropertyName { get; }
        public string OriginalValue { get; }
        public string CurrentValue { get; }

        public EntityDiff(T entity, EntityState state, string propertyName, string originalValue, string currentValue)
        {
            Entity = entity;
            State = state;
            PropertyName = propertyName;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }
    }
}
