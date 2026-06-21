// <copyright file="Program.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using OpenQA.Selenium.BiDi;
    using OpenQA.Selenium.BiDi.Network;
    using OpenQA.Selenium.Firefox;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (args.Length == 0)
            {
                await Console.Error.WriteLineAsync(
                    "Error: Expected at least one argument." + Environment.NewLine +
                    "Usage: " + nameof(Downloader) + " <IMDb URL...>")
                    .ConfigureAwait(false);
                return 1;
            }

            var webDriver = new FirefoxDriver(new FirefoxOptions()
            {
                UseWebSocketUrl = true,
            });
            await using var webDriverConf = webDriver.ConfigureAwait(false);

            var biDi = await webDriver.AsBiDiAsync().ConfigureAwait(false);
            await using var biDiConf = biDi.ConfigureAwait(false);

            // Use GetTreeAsync() to get the current BrowsingContext,
            // instead of .CreateAsync(), which would open a new tab/window.
            // This seems roundabout, but it's what the Selenium tests do:
            // https://github.com/SeleniumHQ/selenium/blob/selenium-4.41.0/dotnet/test/common/BiDi/BiDiFixture.cs#L49
            var initialTree = await biDi.BrowsingContext.GetTreeAsync().ConfigureAwait(false);
            var context = initialTree.Contexts[0].Context;

            var downloader = new Downloader(context)
            {
                TitleFilter = (title) => !File.Exists($"S{title}.jpg"),
            };
            foreach (var arg in args)
            {
                var posters = downloader.DownloadEpisodesAsync(arg).ConfigureAwait(false);
                await foreach (var poster in posters)
                {
                    await SavePoster(poster).ConfigureAwait(false);
                }
            }

            return 0;
        }

        private static async Task SavePoster(
            ImdbPoster poster,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(
                poster.Response.MimeType.StartsWith("image/jpeg", StringComparison.Ordinal),
                "Poster has image/jpeg Content-Type");
            await File.WriteAllBytesAsync(
                    $"S{poster.Title}.jpg",
                    ((Base64BytesValue)poster.Bytes).Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
