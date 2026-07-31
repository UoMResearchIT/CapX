// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;

namespace PPMTool.Services.StatusEvaluators
{
    /// <summary>
    /// Interface for evaluating status messages for a given entity.
    /// </summary>
    public interface IStatusMessageEvaluator<in T>
    {
        /// <summary>
        /// Evaluates the status messages for the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public IReadOnlyList<StatusMessage> GetLatestStatusMessages(T entity);

        /// <summary>
        /// Determines whether the given entity has any active status messages.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasActiveStatusMessages(T entity);

        /// <summary>
        /// Determines whether the given entity has any active error status messages.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasActiveErrorMessages(T entity);
    }
}
