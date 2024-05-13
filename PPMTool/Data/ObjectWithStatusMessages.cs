using System.Collections.Generic;

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
    }
}
