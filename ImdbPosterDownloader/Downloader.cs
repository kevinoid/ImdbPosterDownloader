// <copyright file="Downloader.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using OpenQA.Selenium;
    using OpenQA.Selenium.BiDi;
    using OpenQA.Selenium.BiDi.BrowsingContext;
    using OpenQA.Selenium.BiDi.Network;
    using OpenQA.Selenium.BiDi.Script;
    using OpenQA.Selenium.Firefox;

    using BiDiDataType = OpenQA.Selenium.BiDi.Network.DataType;

    public partial class Downloader : IAsyncDisposable, IDisposable
    {
        private readonly IWebDriver webDriver;
        private readonly IBiDi biDi;
        private readonly BrowsingContext context;
        private bool disposedValue;

        protected Downloader(
            IWebDriver webDriver,
            IBiDi biDi,
            BrowsingContext context)
        {
            this.webDriver = webDriver ?? throw new ArgumentNullException(nameof(webDriver));
            this.biDi = biDi ?? throw new ArgumentNullException(nameof(biDi));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [GeneratedRegex(
            @"^\s*S\s*(?<season>[0-9]+)\.\s*E\s*(?<episode>[0-9]+)\s",
            RegexOptions.CultureInvariant)]
        private static partial Regex EpisodeNumRegex { get; }

        public static Task<Downloader> CreateAsync(
            CancellationToken cancellationToken = default)
        {
            var webDriver = new FirefoxDriver(new FirefoxOptions()
            {
                UseWebSocketUrl = true,
            });

            return CreateAsync(webDriver, cancellationToken);
        }

        public static async Task<Downloader> CreateAsync(
            IWebDriver webDriver,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(webDriver);

            var biDi = await webDriver.AsBiDiAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Use GetTreeAsync() to get the current BrowsingContext,
            // instead of .CreateAsync(), which would open a new tab/window.
            // This seems roundabout, but it's what the Selenium tests do:
            // https://github.com/SeleniumHQ/selenium/blob/selenium-4.41.0/dotnet/test/common/BiDi/BiDiFixture.cs#L49
            var initialTree =
                await biDi.BrowsingContext.GetTreeAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            var context = initialTree.Contexts[0].Context;

            return new Downloader(
                webDriver,
                biDi,
                context);
        }

        public async ValueTask DisposeAsync()
        {
            // Perform async cleanup.
            await this.DisposeAsyncCore().ConfigureAwait(false);

            // Dispose of unmanaged resources.
            this.Dispose(false);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await this.biDi.DisposeAsync().ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.webDriver.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task DownloadEpisodesAsync(
            string episodesUrl,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(episodesUrl);

            var contextCreatedStream =
                await this.biDi.BrowsingContext.ContextCreated.StreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            await using var contextCreatedStreamConf =
                ((IAsyncDisposable)contextCreatedStream).ConfigureAwait(false);
            var contextCreatedEnum = contextCreatedStream.GetAsyncEnumerator(cancellationToken);
            await using var contextCreatedEnumConf = contextCreatedEnum.ConfigureAwait(false);

            var loadStream = await this.context.Load.StreamAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var loadStreamConf = ((IAsyncDisposable)loadStream).ConfigureAwait(false);
            var loadEnum = loadStream.GetAsyncEnumerator(cancellationToken);
            await using var loadEnumConf = loadEnum.ConfigureAwait(false);

            await this.context.NavigateAsync(episodesUrl, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gotChannel = await contextCreatedEnum.MoveNextAsync()
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);
            Debug.Assert(gotChannel, "A new tab was opened");

            while (true)
            {
                var gotLoad = await loadEnum.MoveNextAsync()
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(60), cancellationToken)
                    .ConfigureAwait(false);
                Debug.Assert(gotLoad, "Page load occurred");

                await this.DownloadSeasonAsync(contextCreatedEnum, cancellationToken)
                    .ConfigureAwait(false);

                var nextSeasonBtns = await this.context.LocateNodesAsync(
                        new CssLocator("#next-season-btn"),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var nextSeasonBtn = nextSeasonBtns.Nodes.SingleOrDefault();
                if (nextSeasonBtn == null)
                {
                    break;
                }

                await this.context.ScrollIntoViewAsync(nextSeasonBtn, false, cancellationToken)
                    .ConfigureAwait(false);

                await this.context.ClickAsync(nextSeasonBtn, 0, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task DownloadSeasonAsync(
            IAsyncEnumerator<ContextCreatedEventArgs> contextCreatedEnum,
            CancellationToken cancellationToken)
        {
            // FIXME: Wait for episodes to load, ads to load and page to settle.
            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);

            var episodeLinks = await this.context.LocateNodesAsync(
                    new CssLocator("a.ipc-title-link-wrapper"),
                    new LocateNodesOptions
                    {
                        SerializationOptions = new SerializationOptions
                        {
                            MaxDomDepth = 2,
                        },
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            foreach (var episodeLink in episodeLinks.Nodes)
            {
                await this.DownloadEpisodeAsync(episodeLink, contextCreatedEnum, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task DownloadEpisodeAsync(
            NodeRemoteValue episodeLink,
            IAsyncEnumerator<ContextCreatedEventArgs> contextCreatedEnum,
            CancellationToken cancellationToken)
        {
            var (seasonNum, episodeNum) = ParseEpisodeLink(episodeLink);

            // Scroll the link into view, then click it.
            // Note: Navigating without clicking link causes captcha.
            await this.context.ScrollIntoViewAsync(episodeLink, false, cancellationToken)
                .ConfigureAwait(false);

            // Wait for scroll to complete
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Note: Button 1 (middle) to open in new window
            await this.context.ClickAsync(episodeLink, 1, cancellationToken).ConfigureAwait(false);

            var gotEpisodeContext = await contextCreatedEnum
                .MoveNextUntilAsync(context => context.Parent == null)
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);
            Debug.Assert(gotEpisodeContext, "A new tab was opened for the episode");
            var episodeContext = contextCreatedEnum.Current.Context;

            var episodeLoadStream =
                await episodeContext.Load.StreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            await using var episodeLoadStreamConf =
                ((IAsyncDisposable)episodeLoadStream).ConfigureAwait(false);
            var episodeLoadEnum = episodeLoadStream.GetAsyncEnumerator(cancellationToken);
            await using var episodeLoadEnumConf = episodeLoadEnum.ConfigureAwait(false);

            await episodeContext.ActivateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var gotEpisodeLoad = await episodeLoadEnum.MoveNextAsync()
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(60), cancellationToken)
                .ConfigureAwait(false);
            Debug.Assert(gotEpisodeLoad, "Page load occurred in episode tab");

            var posterLinks = await episodeContext.LocateNodesAsync(
                    new CssLocator("main .ipc-poster > a"),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var posterLink = posterLinks.Nodes.Single();

            var responseCompletedStream =
                await this.context.BiDi.Network.ResponseCompleted.StreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            await using var responseCompletedStreamConf =
                ((IAsyncDisposable)responseCompletedStream).ConfigureAwait(false);

            // Note: BiDiException from Edge if value is above 200,000,000:
            // "invalid argument: Max encoded data size should be between 1 and 200000000"
            var result = await this.context.BiDi.Network.AddDataCollectorAsync(
                    [BiDiDataType.Response],
                    10 * 1024 * 1024,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await using var collectorWrapper =
                new CollectorWrapper(result.Collector).ConfigureAwait(false);

            await episodeContext.ClickAsync(posterLink, 0, cancellationToken)
                .ConfigureAwait(false);

            var gotPosterLoad = await episodeLoadEnum.MoveNextAsync()
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(60), cancellationToken)
                .ConfigureAwait(false);
            Debug.Assert(gotEpisodeLoad, "Page load occurred in episode tab");

            var mediaImgs = await episodeContext.LocateNodesAsync(
                    new CssLocator(".media-viewer img:not(.peek)"),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var lightboxImg = mediaImgs.Nodes.Single();
            var lightboxImgSourceSet = lightboxImg.Value!.GetSourceSet().ToHashSet();

            // Check that all image source URLs are absolute
            foreach (var lightboxImgSource in lightboxImgSourceSet)
            {
                new Uri(lightboxImgSource, UriKind.Absolute);
            }

            var imageResComp =
                await responseCompletedStream.FirstAsync(
                        rc => lightboxImgSourceSet.Contains(rc.Response.Url),
                        cancellationToken)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                    .ConfigureAwait(false);
            var bytes = await this.context.BiDi.Network.GetDataAsync(
                    BiDiDataType.Response,
                    imageResComp.Request.Request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            Debug.Assert(
                imageResComp.Response.MimeType.StartsWith("image/jpeg", StringComparison.Ordinal),
                "Poster has image/jpeg Content-Type");
            await File.WriteAllBytesAsync(
                    $"S{seasonNum:D2}E{episodeNum:D2}-cover.jpg",
                    ((Base64BytesValue)bytes).Value,
                    cancellationToken)
                .ConfigureAwait(false);

            await episodeContext.CloseAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        private static (int Season, int Episode) ParseEpisodeLink(
            NodeRemoteValue episodeLink)
        {
            var episodeLinkDiv = episodeLink.Value!.Children!.Value
                .Single(n => n.Value?.NodeType == (long)XmlNodeType.Element);
            Debug.Assert(
                episodeLinkDiv.Value!.LocalName == "div",
                "Child element of episode link is a <div>");

            var episodeText = episodeLinkDiv.Value!.Children!.Value.Single();
            Debug.Assert(
                episodeText.Value?.NodeType == (long)XmlNodeType.Text,
                "Child node of episode link div is #text");
            var episodeLinkStr = episodeText.Value.NodeValue!;
            var seasonEpNumMatch = EpisodeNumRegex.Match(episodeLinkStr);
            Debug.Assert(
                seasonEpNumMatch.Success,
                "Episode link text has expected format");
            var seasonNum = int.Parse(
                seasonEpNumMatch.Groups["season"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture);
            var episodeNum = int.Parse(
                seasonEpNumMatch.Groups["episode"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture);

            return (seasonNum, episodeNum);
        }
    }
}
