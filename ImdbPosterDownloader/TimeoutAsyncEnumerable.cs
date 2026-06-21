// <copyright file="TimeoutAsyncEnumerable.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>
// Inspired by https://redd.it/1ce18k8 and Claude.

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps an <see cref="IAsyncEnumerable{T}"/> so that each call to
    /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> is limited to
    /// <see cref="Timeout"/> before a <see cref="TimeoutException"/> is
    /// thrown.
    /// </summary>
    /// <typeparam name="T">The type of values to enumerate.</typeparam>
    public sealed class TimeoutAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> source;
        private TimeSpan timeout;

        public TimeoutAsyncEnumerable(IAsyncEnumerable<T> source, TimeSpan timeout)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.Timeout = timeout;
        }

        /// <summary>
        /// Gets or sets the maximum time a single
        /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> call is allowed to take
        /// before a <see cref="TimeoutException"/> is thrown. Changing this takes
        /// effect from the next <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>
        /// call onward; it does not affect a call that is already in flight.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the value is zero or negative.</exception>
        public TimeSpan Timeout
        {
            get => this.timeout;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be positive.");
                }

                this.timeout = value;
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(this, cancellationToken);

        private sealed class Enumerator : IAsyncEnumerator<T>
        {
            private readonly TimeoutAsyncEnumerable<T> owner;
            private readonly CancellationToken externalToken;
            private readonly CancellationTokenSource linkedSource;
            private readonly IAsyncEnumerator<T> inner;

            private bool cleanupHandedOff;
            private bool disposed;

            public Enumerator(TimeoutAsyncEnumerable<T> owner, CancellationToken cancellationToken)
            {
                this.owner = owner;
                this.externalToken = cancellationToken;

                this.linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                this.inner = owner.source.GetAsyncEnumerator(this.linkedSource.Token);
            }

            public T Current => this.inner.Current;

            public async ValueTask<bool> MoveNextAsync()
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);

                TimeSpan timeout = this.owner.Timeout;
                ValueTask<bool> moveNextValueTask = this.inner.MoveNextAsync();
                if (moveNextValueTask.IsCompleted)
                {
                    return await moveNextValueTask.ConfigureAwait(false);
                }

                Task<bool> moveNextTask = moveNextValueTask.AsTask();
                Task delayTask = Task.Delay(timeout, this.externalToken);
                Task winner = await Task.WhenAny(moveNextTask, delayTask).ConfigureAwait(false);

                if (winner == moveNextTask)
                {
                    return await moveNextTask.ConfigureAwait(false);
                }

                // MoveNextAsync is still pending after timeout elapsed.
                // Request cancellation.
                await this.linkedSource.CancelAsync().ConfigureAwait(false);

                // Hand disposal off to a Task to run after the pending call completes.
                // This is necessary because a compiler-generated IAsyncEnumerator throws
                // NotSupportedException if DisposeAsync() is called while a MoveNextAsync()
                // call is still in flight (See <https://stackoverflow.com/a/69847542> and
                // <https://github.com/dotnet/roslyn/issues/77850>.) and waiting for
                // completion here would defeat the timeout if the source ignores cancellation.
                this.cleanupHandedOff = true;
                _ = CleanUpAbandonedEnumeratorAsync(this.inner, moveNextTask, this.linkedSource);

                // Throw if delayTask completed due to caller cancellation, rather than elapsing.
                this.externalToken.ThrowIfCancellationRequested();

                throw new TimeoutException($"Enumeration timed out after {timeout} waiting for the next item.");
            }

            public async ValueTask DisposeAsync()
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;

                if (!this.cleanupHandedOff)
                {
                    await this.inner.DisposeAsync().ConfigureAwait(false);
                    this.linkedSource.Dispose();
                }
            }

            [SuppressMessage(
                "Design",
                "CA1031:Do not catch general exception types",
                Justification = "Dispose must run and throwing from this (unawaited) Task is not useful")]
            [SuppressMessage(
                "ErrorProne.Net.Exceptions",
                "ERP022:UnobservedExceptionInGenericExceptionHandler",
                Justification = "Clean up is best-effort and exceptions are not actionable here")]
            private static async Task CleanUpAbandonedEnumeratorAsync(
                IAsyncEnumerator<T> inner,
                Task<bool> pendingMoveNext,
                CancellationTokenSource linkedCts)
            {
                try
                {
                    // Wait for the abandoned call to actually settle. If the source
                    // honors cancellation this is quick; if it doesn't, the enumerator
                    // simply stays undisposed until it eventually does.
                    await pendingMoveNext.ConfigureAwait(false);
                }
                catch
                {
                    // Whatever the abandoned call throws isn't actionable here.
                }

                try
                {
                    await inner.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
                finally
                {
                    linkedCts.Dispose();
                }
            }
        }
    }
}
