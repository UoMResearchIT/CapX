// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Ganss.Xss;

namespace PPMTool.Services
{
    /// <summary>
    /// HTML sanitisation service for removing potentially dangerous HTML content from user input.
    /// This is important for preventing XSS attacks and ensuring that only safe HTML is stored and displayed.
    /// </summary>
    public class HtmlContentSanitizerService
    {
        /// <summary>
        /// The actual santiser instance
        /// </summary>
        private readonly HtmlSanitizer sanitizer;

        /// <summary>
        /// Constructor that configures the sanitizer to allow certain attributes like "class" and "data-id" while removing potentially harmful content.
        /// </summary>
        public HtmlContentSanitizerService()
        {
            sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("class");

            // This in particular should still all the mention tags in notes to work correctly
            sanitizer.AllowedAttributes.Add("data-id");
        }

        /// <summary>
        /// Sanitises the provided HTML string, removing any potentially harmful content while preserving allowed attributes.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public string Sanitize(string html)
        {
            return string.IsNullOrWhiteSpace(html) ? string.Empty : sanitizer.Sanitize(html);
        }
    }
}
