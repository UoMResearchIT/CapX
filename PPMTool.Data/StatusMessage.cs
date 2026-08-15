// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// A model for a status message
    /// </summary>
    public class StatusMessage
    {
        /// <summary>
        /// The message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The type of message (severity)
        /// </summary>
        public MessageType Type { get; }

        /// <summary>
        /// Conditional expression for when the message should be shown.
        /// </summary>
        public Func<bool>? Condition { get; }

        /// <summary>
        /// Whether the message should be shown or not. Updated by calling Update, which checks the condition.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Feature that is required to make this message relevant
        /// </summary>
        public FeatureType RelevantFeature { get; set; }

        /// <summary>
        /// Create a new status message. Note, the condition will not be immediately checked. Update must be manually called.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="condition"></param>
        /// <param name="relevantFeature"></param>
        public StatusMessage(string message, MessageType type, Func<bool>? condition = null, FeatureType relevantFeature = FeatureType.None)
        {
            Message = message;
            Type = type;
            Condition = condition;
            RelevantFeature = relevantFeature;
        }

        /// <summary>
        /// Evaluate the condition to update the status. If no condition, defaults to true.
        /// </summary>
        public void Update()
        {
            Status = Condition != null ? Condition.Invoke() : true;
        }

        /// <summary>
        /// Type / severity of the message
        /// </summary>
        public enum MessageType
        {
            Success,
            Info,
            Warning,
            Error
        }
    }
}
