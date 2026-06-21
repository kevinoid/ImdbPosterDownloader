// <copyright file="NodePropertiesExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System.Collections.Generic;

    using OpenQA.Selenium.BiDi.Script;

    /// <summary>
    /// Extension methods for <see cref="NodeProperties" />.
    /// </summary>
    internal static class NodePropertiesExtensions
    {
        /// <summary>
        /// Gets the source set for an img node.
        /// </summary>
        /// <param name="nodeProperties">img node for which to get the source set.</param>
        /// <returns>All URLs in src and srcset attributes.</returns>
        /// <seealso href="https://html.spec.whatwg.org/multipage/images.html#source-set" />
        public static IEnumerable<string> GetSourceSet(this NodeProperties nodeProperties)
        {
            if (nodeProperties.Attributes!.TryGetValue("src", out var src)
                && !string.IsNullOrWhiteSpace(src))
            {
                yield return src.Trim(HtmlParserUtils.AsciiWhitespaceChars);
            }

            if (nodeProperties.Attributes!.TryGetValue("srcset", out var srcset)
                && !string.IsNullOrWhiteSpace(srcset))
            {
                foreach (var url in HtmlParserUtils.GetSrcsetUrls(srcset))
                {
                    yield return url;
                }
            }
        }
    }
}
