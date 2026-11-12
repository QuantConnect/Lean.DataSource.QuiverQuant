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
using NUnit.Framework.Legacy;
using QuantConnect.DataProcessing;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class QuiverWallStreetBetsDataDownloaderTests
    {
        private string _root;
        private string _universe;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "quiver-wsb-tests-" + Guid.NewGuid().ToString("N"));
            _universe = Path.Combine(_root, "universe");
            Directory.CreateDirectory(_universe);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        [Test]
        public void RevisedValueReplacesTheStoredRowOfTheSameDate()
        {
            File.WriteAllLines(Path.Combine(_root, "abc.csv"), new[]
            {
                "20260807,5,3,0.2",
                "20260808,10,1,0.1"
            });

            QuiverWallStreetBetsDataDownloader.SaveContentToFile(_root, "ABC", new[]
            {
                "20260808,12,1,0.1",
                "20260809,7,2,-0.3"
            });

            CollectionAssert.AreEqual(new[]
            {
                "20260807,5,3,0.2",
                "20260808,12,1,0.1",
                "20260809,7,2,-0.3"
            }, File.ReadAllLines(Path.Combine(_root, "abc.csv")));
        }

        [Test]
        public void RevisedValueReplacesTheStoredUniverseRowOfTheSameSecurity()
        {
            File.WriteAllLines(Path.Combine(_universe, "20260808.csv"), new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,10,1,0.1",
                "GME SC72NCBXXAHX,GME,50,2,0.5"
            });

            QuiverWallStreetBetsDataDownloader.SaveContentToFile(_universe, "20260808", new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,12,1,0.1"
            });

            CollectionAssert.AreEqual(new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,12,1,0.1",
                "GME SC72NCBXXAHX,GME,50,2,0.5"
            }, File.ReadAllLines(Path.Combine(_universe, "20260808.csv")));
        }

        [Test]
        public void TickersSharingASecurityKeepTheirOwnUniverseRows()
        {
            File.WriteAllLines(Path.Combine(_universe, "20250210.csv"), new[]
            {
                "ABX R735QTJ8XC9X,B,9,50,0.1",
                "ABX R735QTJ8XC9X,GOLD,4,108,0.64043"
            });

            QuiverWallStreetBetsDataDownloader.SaveContentToFile(_universe, "20250210", new[]
            {
                "ABX R735QTJ8XC9X,B,10,49,0.11145",
                "ABX R735QTJ8XC9X,GOLD,4,108,0.64043"
            });

            CollectionAssert.AreEqual(new[]
            {
                "ABX R735QTJ8XC9X,B,10,49,0.11145",
                "ABX R735QTJ8XC9X,GOLD,4,108,0.64043"
            }, File.ReadAllLines(Path.Combine(_universe, "20250210.csv")));
        }

        // The previous merge put the fetched rows before the stored ones and sorted by date with a stable sort,
        // so a stored duplicate pair has the newest value first
        [Test]
        public void DuplicatesAlreadyStoredKeepTheNewestRow()
        {
            File.WriteAllLines(Path.Combine(_root, "abc.csv"), new[]
            {
                "20260808,12,1,0.1",
                "20260808,10,1,0.1"
            });

            QuiverWallStreetBetsDataDownloader.SaveContentToFile(_root, "ABC", new[]
            {
                "20260809,7,2,-0.3"
            });

            CollectionAssert.AreEqual(new[]
            {
                "20260808,12,1,0.1",
                "20260809,7,2,-0.3"
            }, File.ReadAllLines(Path.Combine(_root, "abc.csv")));
        }

        [Test]
        public void DuplicatesAlreadyStoredAreReplacedByTheFetchedRow()
        {
            File.WriteAllLines(Path.Combine(_universe, "20260808.csv"), new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,10,1,0.1",
                "AAPL R735QTJ8XC9X,AAPL,12,1,0.1",
                "GME SC72NCBXXAHX,GME,50,2,0.5"
            });

            QuiverWallStreetBetsDataDownloader.SaveContentToFile(_universe, "20260808", new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,13,1,0.1"
            });

            CollectionAssert.AreEqual(new[]
            {
                "AAPL R735QTJ8XC9X,AAPL,13,1,0.1",
                "GME SC72NCBXXAHX,GME,50,2,0.5"
            }, File.ReadAllLines(Path.Combine(_universe, "20260808.csv")));
        }
    }
}
