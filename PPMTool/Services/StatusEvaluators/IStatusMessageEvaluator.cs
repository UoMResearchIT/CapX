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
        /// Evaluates the status messages for the given entity returning the active ones.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId">Some messages might depend on who is viewing it so optional parameter of the person Id of the viewer</param>
        /// <returns></returns>
        public IReadOnlyList<StatusMessage> GetLatestStatusMessages(T entity, int? messageViewerPersonId = null);

        /// <summary>
        /// Determines whether the given entity has any active status messages.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId">Some messages might depend on who is viewing it so optional parameter of the person Id of the viewer</param>
        /// <returns></returns>
        public bool HasActiveStatusMessages(T entity, int? messageViewerPersonId = null);

        /// <summary>
        /// Determines whether the given entity has any active error status messages.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId">Some messages might depend on who is viewing it so optional parameter of the person Id of the viewer</param>
        /// <returns></returns>
        public bool HasActiveErrorMessages(T entity, int? messageViewerPersonId = null);
    }
}
