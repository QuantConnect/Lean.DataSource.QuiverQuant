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
*/

using NUnit.Framework;
using QuantConnect.DataSource.QuiverQuant;
using QuantConnect.Orders;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class QuiverQuantCsvExtensionsTests
    {
        [TestCase(TransactionCode.Sale, "S")]
        [TestCase(TransactionCode.Purchase, "P")]
        [TestCase(TransactionCode.VoluntaryReport, "V")]
        [TestCase(TransactionCode.GrantOrAward, "A")]
        [TestCase(TransactionCode.DispositionToIssuer, "D")]
        [TestCase(TransactionCode.ExercisePaymentWithSecurities, "F")]
        [TestCase(TransactionCode.DiscretionaryTransaction, "I")]
        [TestCase(TransactionCode.ExerciseOrConversionExempt, "M")]
        [TestCase(TransactionCode.ConversionOfDerivative, "C")]
        [TestCase(TransactionCode.ShortDerivativeExpiration, "E")]
        [TestCase(TransactionCode.LongDerivativeExpirationWithValue, "H")]
        [TestCase(TransactionCode.OutOfMoneyExercise, "O")]
        [TestCase(TransactionCode.InMoneyExercise, "X")]
        [TestCase(TransactionCode.Gift, "G")]
        [TestCase(TransactionCode.SmallAcquisition, "L")]
        [TestCase(TransactionCode.AcquisitionByWill, "W")]
        [TestCase(TransactionCode.VotingTrustDeposit, "Z")]
        [TestCase(TransactionCode.Other, "J")]
        [TestCase(TransactionCode.EquitySwap, "K")]
        [TestCase(TransactionCode.TenderDisposition, "U")]
        public void TransactionCode_RoundTrip(TransactionCode value, string expectedLetter)
        {
            Assert.AreEqual(expectedLetter, value.ToCsv());
            Assert.AreEqual(value, QuiverQuantCsvExtensions.ToTransactionCode(expectedLetter));
        }

        [TestCase("", TransactionCode.Other)]
        [TestCase("?", TransactionCode.Other)]
        [TestCase("unknown", TransactionCode.Other)]
        public void TransactionCode_UnknownInputFallsBackToOther(string input, TransactionCode expected)
        {
            Assert.AreEqual(expected, QuiverQuantCsvExtensions.ToTransactionCode(input));
        }

        [TestCase(OwnershipType.Direct, "D")]
        [TestCase(OwnershipType.Indirect, "I")]
        [TestCase(OwnershipType.Unknown, "")]
        public void OwnershipType_RoundTrip(OwnershipType value, string expectedLetter)
        {
            Assert.AreEqual(expectedLetter, value.ToCsv());
            Assert.AreEqual(value, QuiverQuantCsvExtensions.ToOwnershipType(expectedLetter));
        }

        [TestCase("?", OwnershipType.Unknown)]
        public void OwnershipType_UnknownInputFallsBackToUnknown(string input, OwnershipType expected)
        {
            Assert.AreEqual(expected, QuiverQuantCsvExtensions.ToOwnershipType(input));
        }

        [TestCase(AcquiredDisposedCode.Acquired, "A")]
        [TestCase(AcquiredDisposedCode.Disposed, "D")]
        [TestCase(AcquiredDisposedCode.Unknown, "")]
        public void AcquiredDisposedCode_RoundTrip(AcquiredDisposedCode value, string expectedLetter)
        {
            Assert.AreEqual(expectedLetter, value.ToCsv());
            Assert.AreEqual(value, QuiverQuantCsvExtensions.ToAcquiredDisposedCode(expectedLetter));
        }

        [TestCase("?", AcquiredDisposedCode.Unknown)]
        public void AcquiredDisposedCode_UnknownInputFallsBackToUnknown(string input, AcquiredDisposedCode expected)
        {
            Assert.AreEqual(expected, QuiverQuantCsvExtensions.ToAcquiredDisposedCode(input));
        }

        [TestCase(true, "T")]
        [TestCase(false, "F")]
        [TestCase(null, "")]
        public void NullableBool_RoundTrip(bool? value, string expected)
        {
            Assert.AreEqual(expected, value.ToCsv());
            Assert.AreEqual(value, QuiverQuantCsvExtensions.ToNullableBool(expected));
        }

        [TestCase("anything", null)]
        public void NullableBool_UnknownInputFallsBackToNull(string input, bool? expected)
        {
            Assert.AreEqual(expected, QuiverQuantCsvExtensions.ToNullableBool(input));
        }

        [TestCase(OrderDirection.Buy, "0")]
        [TestCase(OrderDirection.Sell, "1")]
        [TestCase(OrderDirection.Hold, "2")]
        public void OrderDirection_RoundTrip(OrderDirection value, string expected)
        {
            Assert.AreEqual(expected, value.ToCsv());
            Assert.AreEqual(value, QuiverQuantCsvExtensions.ToOrderDirection(expected));
        }

        [TestCase("", OrderDirection.Hold)]
        [TestCase("xyz", OrderDirection.Hold)]
        public void OrderDirection_UnparsableInputFallsBackToHold(string input, OrderDirection expected)
        {
            Assert.AreEqual(expected, QuiverQuantCsvExtensions.ToOrderDirection(input));
        }
    }
}
