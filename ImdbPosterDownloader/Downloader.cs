// <copyright file="Downloader.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using OpenQA.Selenium.BiDi;
    using OpenQA.Selenium.BiDi.BrowsingContext;
    using OpenQA.Selenium.BiDi.Script;

    using BiDiDataType = OpenQA.Selenium.BiDi.Network.DataType;

    public class Downloader(BrowsingContext context)
    {
        private readonly BrowsingContext context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IBiDi biDi = context.BiDi;

        public Predicate<string>? TitleFilter { get; set; }

        public async IAsyncEnumerable<ImdbPoster> DownloadEpisodesAsync(
            string episodesUrl,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(episodesUrl);

            var contextCreatedStream =
                await this.biDi.BrowsingContext.ContextCreated.StreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            await using var contextCreatedStreamConf =
                ((IAsyncDisposable)contextCreatedStream).ConfigureAwait(false);
            var contextCreatedEnum = contextCreatedStream.GetAsyncEnumerator(cancellationToken);
            await using var contextCreatedEnumConf = contextCreatedEnum.ConfigureAwait(false);

            var loadStream = await this.biDi.BrowsingContext.Load.StreamAsync(cancellationToken)
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

                var posters = this.DownloadSeasonAsync(contextCreatedEnum, cancellationToken)
                    .ConfigureAwait(false);
                await foreach (var poster in posters)
                {
                    yield return poster;
                }

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

        private async IAsyncEnumerable<ImdbPoster> DownloadSeasonAsync(
            IAsyncEnumerator<ContextCreatedEventArgs> contextCreatedEnum,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // FIXME: Wait for episodes to load, ads to load and page to settle.
            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);

            // Note: a.ipc-title-link-wrapper also used for heading links:
            // "Contribute to this page"
            // "User lists"
            // "User polls"
            // .episode-item-wrapper ancestor matches only episode title links
            var episodeLinks = await this.context.LocateNodesAsync(
                    new CssLocator(".episode-item-wrapper a.ipc-title-link-wrapper"),
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
                string episodeTitle = GetEpisodeLinkText(episodeLink);

                if (this.TitleFilter == null || this.TitleFilter(episodeTitle))
                {
                    yield return await this.DownloadEpisodeAsync(
                            episodeLink,
                            episodeTitle,
                            contextCreatedEnum,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task<ImdbPoster> DownloadEpisodeAsync(
            NodeRemoteValue episodeLink,
            string episodeTitle,
            IAsyncEnumerator<ContextCreatedEventArgs> contextCreatedEnum,
            CancellationToken cancellationToken)
        {
            // Scroll the link into view, then click it.
            // Note: Navigating without clicking link causes captcha.
            await this.context.ScrollIntoViewAsync(episodeLink, false, cancellationToken)
                .ConfigureAwait(false);

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

            await episodeContext.CloseAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new ImdbPoster(episodeTitle, imageResComp.Response, bytes);
        }

        private static string GetEpisodeLinkText(NodeRemoteValue episodeLink)
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
            return episodeText.Value.NodeValue!;
        }
    }
}
