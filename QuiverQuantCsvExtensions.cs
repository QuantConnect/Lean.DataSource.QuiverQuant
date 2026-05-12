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

using QuantConnect.Orders;

namespace QuantConnect.DataSource.QuiverQuant
{
    /// <summary>
    /// Compact CSV serialization helpers for Quiver enums and primitives. Keeps the
    /// on-disk format short (single SEC letters, 0/1 booleans, -1/0/1 trade direction)
    /// while preserving full enum names in code.
    /// </summary>
    public static class QuiverQuantCsvExtensions
    {
        public static string ToCsv(this TransactionCode value) => value switch
        {
            TransactionCode.Sale => "S",
            TransactionCode.Purchase => "P",
            TransactionCode.VoluntaryReport => "V",
            TransactionCode.GrantOrAward => "A",
            TransactionCode.DispositionToIssuer => "D",
            TransactionCode.ExercisePaymentWithSecurities => "F",
            TransactionCode.DiscretionaryTransaction => "I",
            TransactionCode.ExerciseOrConversionExempt => "M",
            TransactionCode.ConversionOfDerivative => "C",
            TransactionCode.ShortDerivativeExpiration => "E",
            TransactionCode.LongDerivativeExpirationWithValue => "H",
            TransactionCode.OutOfMoneyExercise => "O",
            TransactionCode.InMoneyExercise => "X",
            TransactionCode.Gift => "G",
            TransactionCode.SmallAcquisition => "L",
            TransactionCode.AcquisitionByWill => "W",
            TransactionCode.VotingTrustDeposit => "Z",
            TransactionCode.Other => "J",
            TransactionCode.EquitySwap => "K",
            TransactionCode.TenderDisposition => "U",
            _ => string.Empty,
        };

        public static TransactionCode ToTransactionCode(string value) => value switch
        {
            "S" => TransactionCode.Sale,
            "P" => TransactionCode.Purchase,
            "V" => TransactionCode.VoluntaryReport,
            "A" => TransactionCode.GrantOrAward,
            "D" => TransactionCode.DispositionToIssuer,
            "F" => TransactionCode.ExercisePaymentWithSecurities,
            "I" => TransactionCode.DiscretionaryTransaction,
            "M" => TransactionCode.ExerciseOrConversionExempt,
            "C" => TransactionCode.ConversionOfDerivative,
            "E" => TransactionCode.ShortDerivativeExpiration,
            "H" => TransactionCode.LongDerivativeExpirationWithValue,
            "O" => TransactionCode.OutOfMoneyExercise,
            "X" => TransactionCode.InMoneyExercise,
            "G" => TransactionCode.Gift,
            "L" => TransactionCode.SmallAcquisition,
            "W" => TransactionCode.AcquisitionByWill,
            "Z" => TransactionCode.VotingTrustDeposit,
            "J" => TransactionCode.Other,
            "K" => TransactionCode.EquitySwap,
            "U" => TransactionCode.TenderDisposition,
            _ => TransactionCode.Other,
        };

        public static string ToCsv(this OwnershipType value) => value switch
        {
            OwnershipType.Direct => "D",
            OwnershipType.Indirect => "I",
            _ => string.Empty,
        };

        public static OwnershipType ToOwnershipType(string value) => value switch
        {
            "D" => OwnershipType.Direct,
            "I" => OwnershipType.Indirect,
            _ => OwnershipType.Unknown,
        };

        public static string ToCsv(this AcquiredDisposedCode value) => value switch
        {
            AcquiredDisposedCode.Acquired => "A",
            AcquiredDisposedCode.Disposed => "D",
            _ => string.Empty,
        };

        public static AcquiredDisposedCode ToAcquiredDisposedCode(string value) => value switch
        {
            "A" => AcquiredDisposedCode.Acquired,
            "D" => AcquiredDisposedCode.Disposed,
            _ => AcquiredDisposedCode.Unknown,
        };

        public static string ToCsv(this bool? value) => value switch
        {
            true => "T",
            false => "F",
            null => string.Empty,
        };

        public static bool? ToNullableBool(string value) => value switch
        {
            "T" => true,
            "F" => false,
            _ => null,
        };

        public static string ToCsv(this OrderDirection value) => ((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture);

        public static OrderDirection ToOrderDirection(string value)
        {
            return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? (OrderDirection)parsed
                : OrderDirection.Hold;
        }
    }
}
