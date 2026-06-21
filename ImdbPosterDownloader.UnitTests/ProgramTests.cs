// <copyright file="ProgramTests.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>

namespace ImdbPosterDownloader.UnitTests
{
    using System;
    using System.Threading.Tasks;

    using Xunit;

    public static class ProgramTests
    {
        [Fact]
        public static async Task MainThrowsOnNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("args", () => Program.Main(null!));
        }

        [Fact]
        public static async Task MainReturns1OnEmpty()
        {
            var exitCode = await Program.Main([]);
            Assert.Equal(1, exitCode);
        }
    }
}
