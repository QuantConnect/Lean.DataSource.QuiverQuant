/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.IO;
using NUnit.Framework;
using QuantConnect.DataProcessing;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class QuiverDataDownloaderTests
    {
        private static readonly string Directory = Path.Combine(Path.GetTempPath(), "quiver-rejected-token");

        [Test]
        public void PerDateDownloaderThrowsOnRejectedToken()
        {
            using var downloader = new QuiverCNBCDataDownloader(Directory, Directory, "invalid-token");
            Assert.Throws<UnauthorizedAccessException>(() => downloader.Run(new DateTime(2026, 11, 10)));
        }

        [Test]
        public void BulkDownloaderThrowsOnRejectedToken()
        {
            using var downloader = new QuiverCongressDataDownloader(Directory, "invalid-token");
            Assert.Throws<UnauthorizedAccessException>(() => downloader.Run());
        }
    }
}
