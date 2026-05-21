// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.JSInterop;

namespace PPMTool.Services
{
    /// <summary>
    /// Helper service to allow the setting of CSS variables from C#. Used for dynamic theming (e.g. setting the primary colour).
    /// </summary>
    public class CssVariableService
    {
        private readonly IJSRuntime js;

        public CssVariableService(IJSRuntime js)
        {
            this.js = js;
        }

        /// <summary>
        /// Asynchronously sets the primary color CSS variable to the specified hexadecimal color value.
        /// </summary>
        /// <remarks>This method updates the CSS variable '--rz-primary' in the current document, which
        /// will affect the appearance of UI elements that use this variable.</remarks>
        /// <param name="hex">A string representing the hexadecimal color value to assign to the primary color CSS variable. Must be a
        /// valid CSS hex color code (e.g., "#FF5733").</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SetPrimaryColor(string hex)
        {
            await js.InvokeVoidAsync("setCssVariable", "--rz-primary", hex);
        }
    }
}
