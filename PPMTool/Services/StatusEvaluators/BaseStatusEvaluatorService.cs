// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;

namespace PPMTool.Services.StatusEvaluators
{
    public abstract class BaseStatusEvaluatorService<T> : IStatusMessageEvaluator<T>
    {
        public abstract IReadOnlyList<StatusMessage> GetLatestStatusMessages(T entity);

        /// <summary>
        /// Checks if the entity has any active status messages that are not of type Success.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasActiveStatusMessages(T entity)
        {
            return GetLatestStatusMessages(entity)
                .Any(x => x.Status &&
                          x.Type != StatusMessage.MessageType.Success);
        }

        /// <summary>
        /// Checks if the entity has any active status messages that are of type Error.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasActiveErrorMessages(T entity)
        {
            return GetLatestStatusMessages(entity)
                .Any(x => x.Status &&
                          x.Type == StatusMessage.MessageType.Error);
        }
    }
}