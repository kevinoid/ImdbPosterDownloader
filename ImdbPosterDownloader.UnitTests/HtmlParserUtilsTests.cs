// <copyright file="HtmlParserUtilsTests.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader.UnitTests
{
    using System.Collections.Generic;

    using Xunit;

    public static class HtmlParserUtilsTests
    {
        [Theory]
        [InlineData("", new string[0])]
        [InlineData(" ", new string[0])]
        [InlineData(",", new string[0])]
        [InlineData(" ,", new string[0])]
        [InlineData(" , ", new string[0])]
        [InlineData("https://example.com", new string[] { "https://example.com" })]
        [InlineData(" https://example.com", new string[] { "https://example.com" })]
        [InlineData("https://example.com,", new string[] { "https://example.com" })]
        [InlineData("https://example.com,ma", new string[] { "https://example.com,ma" })]
        [InlineData("https://example.com,ma, ", new string[] { "https://example.com,ma" })]
        [InlineData("https://example.com ,", new string[] { "https://example.com" })]
        [InlineData("https://example.com, ", new string[] { "https://example.com" })]
        [InlineData("https://example.com 12w", new string[] { "https://example.com" })]
        [InlineData("https://example.com 12x", new string[] { "https://example.com" })]
        [InlineData("https://example.com 12w,", new string[] { "https://example.com" })]
        [InlineData("https://example.com, https://example.net", new string[] { "https://example.com", "https://example.net" })]
        [InlineData("https://example.com ,https://example.net", new string[] { "https://example.com", "https://example.net" })]
        [InlineData("https://example.com 12w, https://example.net", new string[] { "https://example.com", "https://example.net" })]
        [InlineData("https://example.com,ma , https://example.net", new string[] { "https://example.com,ma", "https://example.net" })]
        public static void GetSrcsetUrls(string srcset, IEnumerable<string> urls)
        {
            Assert.Equal(urls, HtmlParserUtils.GetSrcsetUrls(srcset));
        }
    }
}
