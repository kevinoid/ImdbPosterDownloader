// <copyright file="MissingPosterException.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using System;

    /// <summary>
    /// The exception that is thrown when the requested work does not have a poster.
    /// </summary>
    public class MissingPosterException : Exception
    {
        public MissingPosterException()
        {
        }

        public MissingPosterException(string message)
            : base(message)
        {
        }

        public MissingPosterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
