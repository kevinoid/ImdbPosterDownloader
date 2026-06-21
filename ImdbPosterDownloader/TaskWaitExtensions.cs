// <copyright file="TaskWaitExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for applying a timeout to a <see cref="Task{T}"/>.
    /// </summary>
    public static class TaskWaitExtensions
    {
        /// <summary>
        /// Ensures a given <see cref="Task{T}"/> completes before a given timeout elapses,
        /// or cancels the given <see cref="CancellationTokenSource"/> and throws
        /// <see cref="TimeoutException"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Task.WaitAsync(TimeSpan)"/> is difficult to use for <see cref="Task{T}"/>s
        /// which require cancel+await before disposal (such as compiler-generated
        /// <see cref="IAsyncEnumerator{T}"/> implementations, which throw
        /// <see cref="NotSupportedException"/> if <see cref="IAsyncDisposable.DisposeAsync"/> is
        /// called while <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> is in progress).  This
        /// extension method handles such situations by cancelling the
        /// <see cref="CancellationTokenSource"/> and awaiting the <see cref="Task{T}"/> after the
        /// timeout expires, then throwing <see cref="TimeoutException"/>.
        /// </remarks>
        /// <typeparam name="T">Type of result produced by <paramref name="task"/>.</typeparam>
        /// <param name="task">Task to which to apply a timeout.</param>
        /// <param name="timeout">Length of time for <paramref name="task"/> to complete before a
        /// <see cref="TimeoutException"/> is thrown.</param>
        /// <param name="cancellationSource">Cancellation token source to cancel after
        /// <paramref name="timeout"/> elapses.  Callers should ensure that
        /// <see cref="CancellationTokenSource.Token"/> affects <paramref name="task"/>.</param>
        /// <returns>A <see cref="Task{T}"/> which completes when <paramref name="task"/> does,
        /// or when <paramref name="timeout"/> elapses.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="timeout"/> is
        /// negative.</exception>
        /// <exception cref="TimeoutException">If <paramref name="timeout"/> elapses before
        /// <paramref name="task"/> completes.</exception>
        public static async Task<T> WaitCancelAsync<T>(
            this Task<T> task,
            TimeSpan timeout,
            CancellationTokenSource cancellationSource)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(cancellationSource);

            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Must be non-negative");
            }

            var delayTask = Task.Delay(timeout);
            var winner = await Task.WhenAny(delayTask, task).ConfigureAwait(false);
            if (winner == task)
            {
                return await task.ConfigureAwait(false);
            }

            await cancellationSource.CancelAsync().ConfigureAwait(false);

            // Await task to ensure completion (as described in method docs above)
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore.  We canceled it.
            }

            throw new TimeoutException($"Timed out after {timeout}.");
        }
    }
}
