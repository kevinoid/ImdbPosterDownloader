// <copyright file="Program.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Threading.Tasks;

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

            var downloader = await Downloader.CreateAsync()
                .ConfigureAwait(false);
            await using var downloaderConf = downloader.ConfigureAwait(false);
            foreach (var arg in args)
            {
                await downloader.DownloadEpisodesAsync(arg)
                    .ConfigureAwait(false);
            }

            return 0;
        }
    }
}
