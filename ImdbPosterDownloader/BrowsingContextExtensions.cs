// <copyright file="BrowsingContextExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using OpenQA.Selenium.BiDi.BrowsingContext;
    using OpenQA.Selenium.BiDi.Input;
    using OpenQA.Selenium.BiDi.Script;

    internal static class BrowsingContextExtensions
    {
        public static async Task<PerformActionsResult> ClickAsync(
            this BrowsingContext context,
            NodeRemoteValue node,
            int button,
            CancellationToken cancellationToken = default)
        {
            return await context.Input.PerformActionsAsync(
                    [
                        new PointerSourceActions(
                            Guid.NewGuid().ToString(),
                            [

                                // FIXME: Element origin is "in-view center point" which might be between lines
                                // https://w3c.github.io/webdriver/#dfn-get-coordinates-relative-to-an-origin
                                new PointerMoveAction(0, 0) { Origin = new ElementOrigin(node) },
                                new PointerDownAction(button),
                                new PointerUpAction(button),
                            ]),
                    ],
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task<RemoteValue> ScrollIntoViewAsync(
            this BrowsingContext context,
            NodeRemoteValue node,
            bool alignToTop,
            CancellationToken cancellationToken = default)
        {
            // FIXME: Scrolling element into view currently requires client script
            // https://github.com/w3c/webdriver-bidi/issues/543
            // https://github.com/w3c/webdriver-bidi/issues/544
            var scrollResult = await context.Script.CallFunctionAsync(
                    "(elem, alignToTop) => elem.scrollIntoView(alignToTop)",
                    false,
                    new CallFunctionOptions
                    {
                        Arguments = [
                            new SharedReferenceLocalValue(node.SharedId),
                            alignToTop,
                        ],
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return scrollResult.AsSuccessResult();
        }
    }
}
