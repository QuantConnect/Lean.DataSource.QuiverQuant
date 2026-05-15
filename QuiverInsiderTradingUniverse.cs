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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.DataSource.QuiverQuant;
using static QuantConnect.StringExtensions;

namespace QuantConnect.DataSource
{
    /// <summary>
    /// Universe Selection helper class for QuiverQuant InsiderTrading dataset
    /// </summary>
    public class QuiverInsiderTradingUniverse : BaseDataCollection
    {
        /// <summary>
        /// Transaction date as reported on SEC Form 4
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Time the transaction was filed and became publicly available
        /// </summary>
        public DateTime FileDate { get; set; }

        /// <summary>
        /// Type of transaction (SEC Form 4 code)
        /// </summary>
        public TransactionCode TransactionCode { get; set; }

        /// <summary>
        /// Reported price per share transacted
        /// </summary>
        public decimal? PricePerShare { get; set; }

        /// <summary>
        /// Number of shares transacted
        /// </summary>
        public decimal? Shares { get; set; }

        /// <summary>
        /// Number of shares owned by insider following the transaction
        /// </summary>
        public decimal? SharesOwnedFollowing { get; set; }

        /// <summary>
        /// Indicates whether transaction was share acquisition or disposal
        /// </summary>
        public AcquiredDisposedCode AcquiredDisposedCode { get; set; }

        /// <summary>
        /// Whether the security is held directly or indirectly
        /// </summary>
        public OwnershipType DirectOrIndirectOwnership { get; set; }

        /// <summary>
        /// Name of the transactor
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Corporate title of the transactor
        /// </summary>
        public string OfficerTitle { get; set; }

        /// <summary>
        /// Whether the transactor is a director of the company
        /// </summary>
        public bool? IsDirector { get; set; }

        /// <summary>
        /// Whether the transactor is an officer of the company
        /// </summary>
        public bool? IsOfficer { get; set; }

        /// <summary>
        /// Whether the transactor is a 10% owner of the company
        /// </summary>
        public bool? IsTenPercentOwner { get; set; }

        /// <summary>
        /// Whether the transactor is not a director, officer, or 10% owner
        /// </summary>
        public bool? IsOther { get; set; }

        /// <summary>
        /// Time the data becomes available to the algorithm
        /// </summary>
        public override DateTime EndTime => Time.AddDays(1);

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "quiver",
                    "insidertrading",
                    "universe",
                    $"{date.ToStringInvariant(DateFormat.EightCharacter)}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.FoldingCollection
            );
        }

        /// <summary>
        /// Parses the data from the line provided and loads it into LEAN
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>New instance</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.Split(',');

            var price = csv[5].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));

            return new QuiverInsiderTradingUniverse
            {
                Time = date.AddDays(-1),
                Symbol = new Symbol(SecurityIdentifier.Parse(csv[0]), csv[1]),
                FileDate = (csv[2].IfNotNullOrEmpty<DateTime?>(s => Parse.DateTimeExact(s, "yyyyMMddHHmmss")) ?? date).AddDays(-1),
                Date = csv[3].IfNotNullOrEmpty<DateTime?>(s => Parse.DateTimeExact(s, "yyyyMMdd")) ?? date.AddDays(-1),
                TransactionCode = QuiverQuantCsvExtensions.ToTransactionCode(csv[4]),
                PricePerShare = price,
                Shares = csv[6].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)),
                SharesOwnedFollowing = csv[7].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)),
                AcquiredDisposedCode = QuiverQuantCsvExtensions.ToAcquiredDisposedCode(csv[8]),
                DirectOrIndirectOwnership = QuiverQuantCsvExtensions.ToOwnershipType(csv[9]),
                Name = csv[10],
                OfficerTitle = csv[11],
                IsDirector = QuiverQuantCsvExtensions.ToNullableBool(csv[12]),
                IsOfficer = QuiverQuantCsvExtensions.ToNullableBool(csv[13]),
                IsTenPercentOwner = QuiverQuantCsvExtensions.ToNullableBool(csv[14]),
                IsOther = QuiverQuantCsvExtensions.ToNullableBool(csv[15]),
                Value = price ?? 0
            };
        }

        /// <summary>
        /// Converts the instance to string
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Symbol}({Time}) :: ") +
                   Invariant($"Date: {Date} ") +
                   Invariant($"FileDate: {FileDate} ") +
                   Invariant($"TransactionCode: {TransactionCode} ") +
                   Invariant($"PricePerShare: {PricePerShare} ") +
                   Invariant($"Shares: {Shares} ") +
                   Invariant($"SharesOwnedFollowing: {SharesOwnedFollowing} ") +
                   Invariant($"AcquiredDisposedCode: {AcquiredDisposedCode} ") +
                   Invariant($"DirectOrIndirectOwnership: {DirectOrIndirectOwnership} ") +
                   Invariant($"Name: {Name} ") +
                   Invariant($"OfficerTitle: {OfficerTitle} ") +
                   Invariant($"IsDirector: {IsDirector} ") +
                   Invariant($"IsOfficer: {IsOfficer} ") +
                   Invariant($"IsTenPercentOwner: {IsTenPercentOwner} ") +
                   Invariant($"IsOther: {IsOther}");
        }

        /// <summary>
        /// Clone implementation
        /// </summary>
        public override BaseData Clone()
        {
            return new QuiverInsiderTradingUniverse()
            {
                Date = Date,
                FileDate = FileDate,
                TransactionCode = TransactionCode,
                PricePerShare = PricePerShare,
                Shares = Shares,
                SharesOwnedFollowing = SharesOwnedFollowing,
                AcquiredDisposedCode = AcquiredDisposedCode,
                DirectOrIndirectOwnership = DirectOrIndirectOwnership,
                Name = Name,
                OfficerTitle = OfficerTitle,
                IsDirector = IsDirector,
                IsOfficer = IsOfficer,
                IsTenPercentOwner = IsTenPercentOwner,
                IsOther = IsOther,
                Data = Data,
                Symbol = Symbol,
                Time = Time,
            };
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Gets the supported resolution for this data and security type
        /// </summary>
        public override List<Resolution> SupportedResolutions()
        {
            return DailyResolution;
        }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="T:NodaTime.DateTimeZone" /> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }
    }
}
