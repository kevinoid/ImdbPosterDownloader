// <copyright file="ImdbPoster.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader
{
    using OpenQA.Selenium.BiDi.Network;

    public record ImdbPoster(
        string Title,
        ResponseData Response,
        BytesValue Bytes);
}
