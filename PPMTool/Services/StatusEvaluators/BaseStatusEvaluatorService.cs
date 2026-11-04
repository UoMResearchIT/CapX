// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;
using PPMTool.Data.Enums;

namespace PPMTool.Services.StatusEvaluators
{
    public abstract class BaseStatusEvaluatorService<T> : IStatusMessageEvaluator<T>
    {
        protected BaseStatusEvaluatorService(FeatureService featureService)
        {
            this.featureService = featureService;
        }

        protected readonly FeatureService featureService;

        /// <summary>
        /// Builds the core status messages and their conditions for the entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId"></param>
        /// <returns></returns>
        protected abstract IReadOnlyList<StatusMessage> BuildCoreStatusMessages(T entity, int? messageViewerPersonId = null);

        /// <inheritdoc />
        public IReadOnlyList<StatusMessage> GetLatestStatusMessages(T entity, int? messageViewerPersonId = null)
        {
            // Build messages
            var messages = BuildCoreStatusMessages(entity, messageViewerPersonId).ToList();

            // Evaluate the conditions
            foreach (var message in messages)
            {
                message.Update();
            }

            // Only messages relevant to currently enabled features should
            // suppress the default success message.
            var hasActiveRelevantMessage = messages.Any(message =>
                message.Status &&
                message.Type != StatusMessage.MessageType.Success &&
                IsRelevant(message));

            if (!hasActiveRelevantMessage)
            {
                var successMessage = new StatusMessage(
                    "Everything looks OK!",
                    StatusMessage.MessageType.Success);

                successMessage.Update();

                return [successMessage];
            }

            // Otherwise return the messages
            return messages;
        }

        /// <summary>
        /// Checks if the entity has any active status messages that are not of type Success.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId"></param>
        /// <returns></returns>
        public bool HasActiveStatusMessages(T entity, int? messageViewerPersonId = null)
        {
            return GetLatestStatusMessages(entity, messageViewerPersonId)
                .Any(x => x.Status &&
                          x.Type != StatusMessage.MessageType.Success);
        }

        /// <summary>
        /// Checks if the entity has any active status messages that are of type Error.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messageViewerPersonId"></param>
        /// <returns></returns>
        public bool HasActiveErrorMessages(T entity, int? messageViewerPersonId = null)
        {
            return GetLatestStatusMessages(entity, messageViewerPersonId)
                .Any(x => x.Status &&
                          x.Type == StatusMessage.MessageType.Error);
        }

        /// <summary>
        /// Method to determine whether a status message is relevant based on activated features
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool IsRelevant(StatusMessage message)
        {
            return message.RelevantFeature == FeatureType.None ||
                   featureService.IsFeatureEnabled(message.RelevantFeature);
        }
    }
}