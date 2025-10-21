using Radzen;

namespace PPMTool.Services
{
    public class LayoutActionBarService
    {
        public event Action OnChange;

        /// <summary>
        /// Whether there are any buttons or error messages to show in the action bar.
        /// </summary>
        public bool ShowActionBar => Buttons.Any() || !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Error message to show
        /// </summary>
        public string ErrorMessage { get; private set; } = "";

        // Action buttons to show in the action bar
        public List<ActionButton> Buttons { get; } = new();

        /// <summary>
        /// Helper to set buttons
        /// </summary>
        /// <param name="buttons"></param>
        public void SetButtons(IEnumerable<ActionButton> buttons)
        {
            Buttons.Clear();
            Buttons.AddRange(buttons);
            OnChange?.Invoke();
        }

        /// <summary>
        /// Set the error message
        /// </summary>
        /// <param name="message"></param>
        public void SetErrorMessage(string message)
        {
            ErrorMessage = message;
            OnChange?.Invoke();
        }

        /// <summary>
        /// Reset the action bar completely to initial state
        /// </summary>
        public void Reset()
        {
            Buttons.Clear();
            ErrorMessage = "";
            OnChange?.Invoke();
        }

        /// <summary>
        /// Clears the error message
        /// </summary>
        public void ClearErrorMessage()
        {
            ErrorMessage = "";
            OnChange?.Invoke();
        }
    }

    /// <summary>
    /// Model of an action button
    /// </summary>
    public class ActionButton
    {
        /// <summary>
        /// Text on the button
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Icon on the button
        /// </summary>
        public string Icon { get; set; } = "";

        /// <summary>
        /// Action to invoke when clicked
        /// </summary>
        public Action OnClick { get; set; }

        /// <summary>
        /// Style of the button
        /// </summary>
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;
    }
}
