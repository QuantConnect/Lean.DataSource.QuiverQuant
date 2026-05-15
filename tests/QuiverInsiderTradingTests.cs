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
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.DataProcessing;
using QuantConnect.DataSource;
using QuantConnect.DataSource.QuiverQuant;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class QuiverInsiderTradingTests
    {
        [Test]
        public void JsonRoundTrip()
        {
            var expected = CreateNewInstance();
            var type = expected.GetType();
            var serialized = JsonConvert.SerializeObject(expected);
            var result = JsonConvert.DeserializeObject(serialized, type);

            AssertAreEqual(expected, result);
        }

        [Test]
        public void Clone()
        {
            var expected = CreateNewInstance();
            var result = expected.Clone();

            AssertAreEqual(expected, result);
        }

        [Test]
        public void Reader_ParsesCompactFormat()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverInsiderTrading();
            var line = "20260508,20260507093000,20260507,P,150.25,100,500,A,D,John Smith,CEO,T,T,F,";

            var result = (QuiverInsiderTrading)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(symbol, result.Symbol);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
            Assert.AreEqual(new DateTime(2026, 5, 8), result.EndTime);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Date);
            Assert.AreEqual(new DateTime(2026, 5, 6, 9, 30, 0), result.FileDate);
            Assert.AreEqual(TransactionCode.Purchase, result.TransactionCode);
            Assert.AreEqual(150.25m, result.PricePerShare);
            Assert.AreEqual(100m, result.Shares);
            Assert.AreEqual(500m, result.SharesOwnedFollowing);
            Assert.AreEqual(AcquiredDisposedCode.Acquired, result.AcquiredDisposedCode);
            Assert.AreEqual(OwnershipType.Direct, result.DirectOrIndirectOwnership);
            Assert.AreEqual("John Smith", result.Name);
            Assert.AreEqual("CEO", result.OfficerTitle);
            Assert.AreEqual(true, result.IsDirector);
            Assert.AreEqual(true, result.IsOfficer);
            Assert.AreEqual(false, result.IsTenPercentOwner);
            Assert.IsNull(result.IsOther);
        }

        [Test]
        public void Reader_EmptyFileDateFallsBackToUploadedMinusOne()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverInsiderTrading();
            // csv[1] (fileDate) is empty — Reader uses uploadedDate.AddDays(-1)
            var line = "20260508,,20260507,S,275,1534,13366,D,D,Jane Doe,CFO,,T,,";

            var result = (QuiverInsiderTrading)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(new DateTime(2026, 5, 7), result.FileDate);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
        }

        [Test]
        public void Reader_EmptyDateFallsBackToUploadedMinusOne()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverInsiderTrading();
            // csv[2] (Date) is empty — Reader falls back to uploadedDate.AddDays(-1)
            var line = "20260508,,,,,1717,40879,,,Jane Doe,,,,,";

            var result = (QuiverInsiderTrading)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(new DateTime(2026, 5, 7), result.Date);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.FileDate);
        }

        [Test]
        public void Reader_EmptyOptionalFieldsAreNull()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverInsiderTrading();
            // All optional numerics/booleans empty
            var line = "20260508,,20260507,M,,1717,40879,A,D,,,,,,";

            var result = (QuiverInsiderTrading)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(TransactionCode.ExerciseOrConversionExempt, result.TransactionCode);
            Assert.IsNull(result.PricePerShare);
            Assert.AreEqual(1717m, result.Shares);
            Assert.AreEqual(40879m, result.SharesOwnedFollowing);
            Assert.AreEqual(string.Empty, result.Name);
            Assert.AreEqual(string.Empty, result.OfficerTitle);
            Assert.IsNull(result.IsDirector);
            Assert.IsNull(result.IsOfficer);
            Assert.IsNull(result.IsTenPercentOwner);
            Assert.IsNull(result.IsOther);
        }

        [Test]
        public void UniverseReader_ParsesCompactFormat()
        {
            var factory = new QuiverInsiderTradingUniverse();
            // csv[0]=sid, csv[1]=ticker, csv[2]=fileDate(empty -> fallback), csv[3]=Date, csv[4]=TransactionCode, ...
            var line = "AAPL R735QTJ8XC9X,AAPL,,20260507,P,150.25,100,500,A,D,John Smith,CEO,T,T,F,";

            var result = (QuiverInsiderTradingUniverse)factory.Reader(null, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual("AAPL", result.Symbol.Value);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.FileDate);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Date);
            Assert.AreEqual(TransactionCode.Purchase, result.TransactionCode);
            Assert.AreEqual(150.25m, result.PricePerShare);
            Assert.AreEqual(AcquiredDisposedCode.Acquired, result.AcquiredDisposedCode);
            Assert.AreEqual(OwnershipType.Direct, result.DirectOrIndirectOwnership);
            Assert.AreEqual("John Smith", result.Name);
            Assert.AreEqual("CEO", result.OfficerTitle);
            Assert.AreEqual(150.25m, result.Value);
        }

        private static SubscriptionDataConfig CreateConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(
                typeof(QuiverInsiderTrading), symbol, Resolution.Daily,
                DateTimeZone.Utc, DateTimeZone.Utc, false, false, false);
        }

        [TestCase("abc123:msft\"", ExpectedResult = new string[] {"MSFT"})]
        [TestCase("AAPL+", ExpectedResult = new string[] {"AAPL"})]
        [TestCase("AAPL-", ExpectedResult = new string[] {"AAPL"})]
        [TestCase("AAPL=", ExpectedResult = new string[] {"AAPL"})]
        [TestCase("GOOG|C", ExpectedResult = new string[] {"GOOG"})]
        [TestCase("A_", ExpectedResult = new string[] {"A"})]
        [TestCase("CRDA CRDB", ExpectedResult = new string[] {"CRDA", "CRDB"})]
        [TestCase("AAPL", ExpectedResult = new string[] {"AAPL"})]
        public string[] TryNormalizeDefunctTicker(string rawTicker)
        {
            var testDownloader = new TestDownloader();
            return testDownloader.TestTryNormalizeDefunctTicker(rawTicker);
        }

        private void AssertAreEqual(object expected, object result, bool filterByCustomAttributes = false)
        {
            foreach (var propertyInfo in expected.GetType().GetProperties())
            {
                // we skip Symbol which isn't protobuffed
                if (filterByCustomAttributes && propertyInfo.CustomAttributes.Count() != 0)
                {
                    Assert.AreEqual(propertyInfo.GetValue(expected), propertyInfo.GetValue(result));
                }
            }
            foreach (var fieldInfo in expected.GetType().GetFields())
            {
                Assert.AreEqual(fieldInfo.GetValue(expected), fieldInfo.GetValue(result));
            }
        }

        private BaseData CreateNewInstance()
        {
            return new QuiverInsiderTrading
            {
                Symbol = Symbol.Empty,
                Time = DateTime.Today,
                DataType = MarketDataType.Base,
                Date = DateTime.Today,
                FileDate = DateTime.Today,
                TransactionCode = TransactionCode.Purchase,
                Shares = 0.0m,
                PricePerShare = 0.0m,
                SharesOwnedFollowing = 0.0m,
                AcquiredDisposedCode = AcquiredDisposedCode.Acquired,
                DirectOrIndirectOwnership = OwnershipType.Direct,
                Name = "John Smith",
                OfficerTitle = "CEO",
                IsDirector = false,
                IsOfficer = true,
                IsTenPercentOwner = false,
                IsOther = false,
            };
        }

        public class TestDownloader : QuiverInsiderTradingDataDownloader
        {
            public TestDownloader()
                : base()
            {
            }

            public string[] TestTryNormalizeDefunctTicker(string rawTicker)
            {
                TryNormalizeDefunctTicker(rawTicker, out var tickerList);
                return tickerList;
            }
        }
    }
}