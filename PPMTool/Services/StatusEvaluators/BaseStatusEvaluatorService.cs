// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;

namespace PPMTool.Services.StatusEvaluators
{
    public abstract class BaseStatusEvaluatorService<T> : IStatusMessageEvaluator<T>
    {
        /// <summary>
        /// Builds the core status messages and their conditions for the entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract IReadOnlyList<StatusMessage> BuildCoreStatusMessages(T entity);

        /// <inheritdoc />
        public IReadOnlyList<StatusMessage> GetLatestStatusMessages(T entity)
        {
            // Build messages
            var messages = BuildCoreStatusMessages(entity).ToList();

            // Evaluate the conditions
            foreach (var message in messages)
            {
                message.Update();
            }

            // If there are no active messages that are not of type Success, return a default success message
            if (!messages.Any(x =>
                x.Status &&
                x.Type != StatusMessage.MessageType.Success))
            {
                // Add message then call update to set status to true
                var message = new StatusMessage("Everything looks OK!", StatusMessage.MessageType.Success);
                message.Update();
                return
                [
                    message
                ];
            }

            // Otherwise return the messages
            return messages;
        }

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