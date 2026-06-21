// <copyright file="IAsyncEnumeratorExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="IAsyncEnumerator{T}" />.
    /// </summary>
    internal static class IAsyncEnumeratorExtensions
    {
        public static async Task<bool> MoveNextUntilAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Predicate<T> predicate)
        {
            bool moved;
            do
            {
                moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            while (moved && !predicate(enumerator.Current));

            return moved;
        }
    }
}
