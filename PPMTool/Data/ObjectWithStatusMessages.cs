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
            UpdateStatusMessages();
            return statusMessages;
        }

        /// <summary>
        /// Update the status messages based on their conditions
        /// </summary>
        public virtual void UpdateStatusMessages()
        {
            foreach (var item in statusMessages)
            {
                item.Update();
            }
        }

        /// <summary>
        /// Checks whether this project has any active status messages trigger by its own state or states of the subtasks
        /// </summary>
        /// <returns></returns>
        public bool HasActiveStatusMessages()
        {
            return statusMessages.Any(x => x.Status && x.Type != StatusMessage.MessageType.Success);
        }

        /// <summary>
        /// Checks whether this project has any error-grade status messages
        /// </summary>
        /// <returns></returns>
        public bool HasActiveErrorMessages()
        {
            return statusMessages.Any(x => x.Status && x.Type == StatusMessage.MessageType.Error);
        }
    }
}
