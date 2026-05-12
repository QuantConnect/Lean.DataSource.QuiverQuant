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
using QuantConnect.DataSource;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class QuiverCNBCTests
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
        public void CloneCollection()
        {
            var expected = CreateNewCollectionInstance();
            var result = expected.Clone();

            AssertAreEqual(expected, result);
        }

        [Test]
        public void Reader_ParsesCompactFormat()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverCNBC();
            // csv: uploadDate, adviceDate, direction(0=Buy/1=Sell/2=Hold), traders, notes
            var line = "20260508,20260507,0,Jim Cramer,catalyst";

            var result = (QuiverCNBC)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(symbol, result.Symbol);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
            Assert.AreEqual(new DateTime(2026, 5, 8), result.EndTime);
            Assert.AreEqual(new DateTime(2026, 5, 6), result.AdviceDate);
            Assert.AreEqual(OrderDirection.Buy, result.Direction);
            Assert.AreEqual("Jim Cramer", result.Traders);
            Assert.AreEqual("catalyst", result.Notes);
        }

        [Test]
        public void Reader_EmptyAdviceDateFallsBackToUploadedMinusOne()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverCNBC();
            var line = "20260508,,1,Steve Weiss,";

            var result = (QuiverCNBC)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.AdviceDate);
            Assert.AreEqual(OrderDirection.Sell, result.Direction);
            Assert.AreEqual(string.Empty, result.Notes);
        }

        [Test]
        public void Reader_MissingTrailingNotesDefaultsToEmpty()
        {
            var symbol = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var config = CreateConfig(symbol);
            var factory = new QuiverCNBC();
            // 4 columns only — trailing notes column missing entirely
            var line = "20260508,20260507,2,Rob Sechan";

            var result = (QuiverCNBC)factory.Reader(config, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual(OrderDirection.Hold, result.Direction);
            Assert.AreEqual("Rob Sechan", result.Traders);
            Assert.AreEqual(string.Empty, result.Notes);
        }

        [Test]
        public void UniverseReader_ParsesCompactFormat()
        {
            var factory = new QuiverCNBCsUniverse();
            // csv: sid, ticker, adviceDate, direction, traders, notes
            var line = "AAPL R735QTJ8XC9X,AAPL,20260507,0,Jim Cramer,catalyst";

            var result = (QuiverCNBCsUniverse)factory.Reader(null, line, new DateTime(2026, 5, 8), false);

            Assert.AreEqual("AAPL", result.Symbol.Value);
            Assert.AreEqual(new DateTime(2026, 5, 7), result.Time);
            Assert.AreEqual(new DateTime(2026, 5, 6), result.AdviceDate);
            Assert.AreEqual(OrderDirection.Buy, result.Direction);
            Assert.AreEqual("Jim Cramer", result.Traders);
            Assert.AreEqual("catalyst", result.Notes);
        }

        private static SubscriptionDataConfig CreateConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(
                typeof(QuiverCNBC), symbol, Resolution.Daily,
                DateTimeZone.Utc, DateTimeZone.Utc, false, false, false);
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
            return new QuiverCNBC
            {
                Symbol = Symbol.Empty,
                Time = DateTime.Today,
                DataType = MarketDataType.Base,
                Notes = "N/a",
                Direction = OrderDirection.Buy,
                Traders = "Jim Cramer"
            };
        }

        private BaseDataCollection CreateNewCollectionInstance()
        {
            return new QuiverCNBCs
            {
                new QuiverCNBC
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Today,
                    DataType = MarketDataType.Base,
                    Notes = "N/a",
                    Direction = OrderDirection.Buy,
                    Traders = "Jim Cramer"
                },
                new QuiverCNBC
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Today,
                    DataType = MarketDataType.Base,
                    Notes = "N/a",
                    Direction = OrderDirection.Buy,
                    Traders = "Jim Cramer"
                }
            };
        }
    }
}
