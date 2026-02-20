// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Collections.Generic;
using System.Linq;

namespace PPMTool.Data
{
    public abstract class ObjectWithStatusMessages
    {
        /// <summary>
        /// List of status messages
        /// </summary>
        protected IList<StatusMessage> statusMessages;

        /// <summary>
        /// Calls update on the status messages in the list and returns the updated list
        /// </summary>
        /// <returns></returns>
        public virtual IList<StatusMessage> GetLatestStatusMessages()
        {
            if (statusMessages != null)
            {
                foreach (var item in statusMessages)
                {
                    item.Update();
                }
            }
            return statusMessages;
        }

        /// <summary>
        /// Checks whether this project has any active status messages trigger by its own state or states of the subtasks
        /// </summary>
        /// <returns></returns>
        public bool HasActiveStatusMessages()
        {
            return statusMessages?.Any(x => x.Status && x.Type != StatusMessage.MessageType.Success) ?? false;
        }

        /// <summary>
        /// Checks whether this project has any error-grade status messages
        /// </summary>
        /// <returns></returns>
        public bool HasActiveErrorMessages()
        {
            return statusMessages?.Any(x => x.Status && x.Type == StatusMessage.MessageType.Error) ?? false;
        }
    }
}
