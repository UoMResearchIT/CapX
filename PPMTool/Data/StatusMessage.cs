namespace PPMTool.Data
{
    public class StatusMessage
    {
        public string Message { get; }

        public MessageType Type { get; }

        public Func<bool> Condition { get; }

        public bool Status { get; private set; }

        /// <summary>
        /// Create a new status message. Note, the condition will not be immediately checked. Update must be manually called.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="condition"></param>
        public StatusMessage(string message, MessageType type, Func<bool> condition = null)
        {
            Message = message;
            Type = type;
            Condition = condition;
        }

        public void Update()
        {
            Status = Condition != null ? Condition.Invoke() : false;
        }

        public enum MessageType
        {
            Success,
            Info,
            Warning,
            Error
        }
    }
}
