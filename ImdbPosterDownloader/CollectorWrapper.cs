// <copyright file="CollectorWrapper.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Threading.Tasks;

    using OpenQA.Selenium.BiDi.Network;

    internal sealed class CollectorWrapper(Collector collector) : IAsyncDisposable
    {
        private bool disposedValue;

        public Collector Collector => collector;

        public async ValueTask DisposeAsync()
        {
            if (!this.disposedValue)
            {
                await collector.BiDi.Network.RemoveDataCollectorAsync(collector)
                    .ConfigureAwait(false);
                this.disposedValue = true;
            }
        }
    }
}
