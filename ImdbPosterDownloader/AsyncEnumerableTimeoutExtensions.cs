// <copyright file="AsyncEnumerableTimeoutExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods to add timeouts to <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static class AsyncEnumerableTimeoutExtensions
    {
        /// <summary>
        /// Wraps an <see cref="IAsyncEnumerable{T}"/> in a <see cref="TimeoutAsyncEnumerable{T}"/>
        /// so that calls to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> are limited to a given
        /// timeout.
        /// </summary>
        /// <remarks>
        /// Compiler-generated <see cref="IAsyncEnumerator{T}"/> implementations only support one
        /// in-flight operation at a time: calling <see cref="IAsyncDisposable.DisposeAsync"/>
        /// while a previous <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> call hasn't completed
        /// throws <see cref="NotSupportedException"/>, making cancellation on timeout (e.g. using
        /// <see cref="Task.WaitAsync(TimeSpan)"/>) difficult and error-prone.
        /// </remarks>
        /// <typeparam name="T">Type of items enumerated by <paramref name="source"/>.</typeparam>
        /// <param name="source">Async enumerable on which to apply timeouts.</param>
        /// <param name="timeout">Length of time for <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>
        /// to complete before a <see cref="TimeoutException"/> is thrown.</param>
        /// <returns>A <see cref="TimeoutAsyncEnumerable{T}"/> wrapping <paramref name="source"/>.</returns>
        public static TimeoutAsyncEnumerable<T> WithTimeout<T>(
            this IAsyncEnumerable<T> source,
            TimeSpan timeout)
            => new(source, timeout);
    }
}
