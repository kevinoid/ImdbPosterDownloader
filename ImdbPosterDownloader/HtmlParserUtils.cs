// <copyright file="HtmlParserUtils.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System.Collections.Generic;

    /// <summary>
    /// Utility methods for parsing HTML.
    /// </summary>
    internal static class HtmlParserUtils
    {
        /// <summary>
        /// <a href="https://infra.spec.whatwg.org/#ascii-whitespace">ASCII Whitespace</a>, as
        /// // defined for HTML.
        /// </summary>
        internal static readonly char[] AsciiWhitespaceChars = ['\t', '\n', '\f', '\r', ' '];

        /// <summary>
        /// Gets URLs from a <c>srcset</c> attribute.
        /// </summary>
        /// <param name="srcset">Value of <c>srcset</c> attribute from which to get URLs.</param>
        /// <returns>All URLs in <paramref name="srcset" />.</returns>
        public static IEnumerable<string> GetSrcsetUrls(string srcset)
        {
            // https://html.spec.whatwg.org/multipage/images.html#parsing-a-srcset-attribute
            int startPos = 0;
            while (startPos < srcset.Length)
            {
                int nextSpace = srcset.IndexOfAny(AsciiWhitespaceChars, startPos);
                if (nextSpace == startPos)
                {
                    // Skip whitespace at start of candidate
                    startPos += 1;
                    continue;
                }

                var url = nextSpace == -1 ? srcset[startPos..] : srcset[startPos..nextSpace];
                var urlTrimmed = url.TrimEnd(',');

                if (urlTrimmed.Length != 0)
                {
                    yield return urlTrimmed;
                }

                if (nextSpace == -1)
                {
                    break;
                }

                if (!ReferenceEquals(url, urlTrimmed))
                {
                    // Comma was removed from url, so next candidate starts after end of URL
                    startPos = nextSpace + 1;
                }
                else
                {
                    // Comma was not removed, so next candidate starts after next comma
                    var nextComma = srcset.IndexOf(',', nextSpace + 1);
                    if (nextComma == -1)
                    {
                        break;
                    }

                    startPos = nextComma + 1;
                }
            }
        }
    }
}
