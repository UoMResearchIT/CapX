// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Collections.Generic;
using PPMTool.Data.Context;

namespace PPMTool.Services
{
    public interface IEntityService<T>
    {
        public abstract int Add(PPMToolContext context, T entity, bool commitChanges = true);
        public IEnumerable<T> GetAll(PPMToolContext context);
        public int Update(PPMToolContext context, T entity, bool commitChanges = true);
        public void Delete(PPMToolContext context, T entity, bool commitChanges = true);
        public void RestoreModel<U>(PPMToolContext context, ref U entity);

    }
}
