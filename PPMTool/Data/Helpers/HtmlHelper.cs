using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Xceed.Words.NET;

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Taken from https://stackoverflow.com/questions/286813/how-do-you-convert-html-to-plain-text
    /// </summary>
    public abstract class HtmlHelper
    {
        /// <summary>
        /// Converts HTML to plain text / strips tags.
        /// </summary>
        /// <param name="html">The HTML</param>
        /// <returns></returns>
        public static string ConvertToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        private static void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        private static void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if (parentName == "script" || parentName == "style")
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                        case "br":
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }

        /// <summary>
        /// Test whether a link is valid
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static bool IsValidLink(string test)
        {
            return !string.IsNullOrWhiteSpace(test) && test.StartsWith("http") && test.Length >= 12 && Uri.TryCreate(test, UriKind.Absolute, out _);
        }

        /// <summary>
        /// Method to take HTML with simple tags and convert it to plain text and insert it as a paragraph in the DocX document provided
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="html"></param>
        /// <param name="styleId"></param>
        public static void InsertHtmlLikeTextWithLinks(DocX doc, string html, string styleId)
        {
            if (string.IsNullOrEmpty(html)) return;

            // Replace <br> and <div> with newlines
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<div.*?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</div>", "\n", RegexOptions.IgnoreCase);

            // Replace <li> with bullet points
            html = Regex.Replace(html, @"<li.*?>", "• ", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</li>", "\n", RegexOptions.IgnoreCase);

            // Replace <ol> with newlines
            html = Regex.Replace(html, @"<ol.*?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</ol>", "\n", RegexOptions.IgnoreCase);

            // Decode HTML entities
            html = html.Replace("&nbsp;", " ");
            html = html.Replace("&quot;", "\"");

            // Match and insert hyperlinks
            var linkRegex = new Regex(@"<a\s+href\s*=\s*[""'](?<url>[^""']+)[""'].*?>(?<text>.*?)</a>", RegexOptions.IgnoreCase);
            int lastIndex = 0;

            // Start paragraph
            var paragraph = doc.InsertParagraph();
            paragraph.StyleId = styleId;

            foreach (Match match in linkRegex.Matches(html))
            {
                // Insert text before the link
                if (match.Index > lastIndex)
                {
                    string beforeLink = html.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Append(beforeLink.Trim());
                }

                // Insert the hyperlink
                string url = match.Groups["url"].Value;
                string linkText = match.Groups["text"].Value;

                var hyperlink = doc.AddHyperlink(linkText, new Uri(url));

                paragraph.Append(" ");
                paragraph.AppendHyperlink(hyperlink);

                lastIndex = match.Index + match.Length;
            }

            // Insert any remaining text after the last link
            if (lastIndex < html.Length)
            {
                string remaining = html.Substring(lastIndex).TrimStart();

                // Add a space before remaining text if it doesn't start with punctuation
                if (string.IsNullOrEmpty(remaining) || !char.IsPunctuation(remaining[0]))
                {
                    paragraph.Append(" ");
                }

                paragraph.Append(remaining);
            }

        }
    }
}
