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

using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.DataSource.QuiverQuant;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace QuantConnect.DataSource
{
    /// <summary>
    /// Insider Trading by private businesses
    /// </summary>
    [JsonObject]
    public class QuiverInsiderTrading : BaseDataCollection
    {
        /// <summary>
        /// Transaction date as reported on SEC Form 4
        /// </summary>
        [JsonProperty(PropertyName = "Date")]
        [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Time the transaction was filed and became publicly available
        /// </summary>
        [JsonProperty(PropertyName = "fileDate")]
        public DateTime FileDate { get; set; }

        /// <summary>
        /// Type of transaction (see SEC Form 4 codes:
        /// https://www.sec.gov/files/forms-3-4-5.pdf)
        /// </summary>
        [JsonProperty(PropertyName = "TransactionCode")]
        public TransactionCode TransactionCode { get; set; }

        /// <summary>
        /// Reported price per share transacted
        /// </summary>
        [JsonProperty(PropertyName = "PricePerShare")]
        public decimal? PricePerShare { get; set; }

        /// <summary>
        /// Number of shares transacted
        /// </summary>
        [JsonProperty(PropertyName = "Shares")]
        public decimal? Shares { get; set; }

        /// <summary>
        /// Number of shares owned by insider following the transaction
        /// </summary>
        [JsonProperty(PropertyName = "SharesOwnedFollowing")]
        public decimal? SharesOwnedFollowing { get; set; }

        /// <summary>
        /// Indicates whether transaction was share acquisition or disposal
        /// </summary>
        [JsonProperty(PropertyName = "AcquiredDisposedCode")]
        public AcquiredDisposedCode AcquiredDisposedCode { get; set; }

        /// <summary>
        /// Whether the security is held directly or indirectly by the reporting person
        /// </summary>
        [JsonProperty(PropertyName = "directOrIndirectOwnership")]
        public OwnershipType DirectOrIndirectOwnership { get; set; }

        /// <summary>
        /// Name of the transactor
        /// </summary>
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Corporate title of the transactor
        /// </summary>
        [JsonProperty(PropertyName = "officerTitle")]
        public string OfficerTitle { get; set; }

        /// <summary>
        /// Whether the transactor is a director of the company
        /// </summary>
        [JsonProperty(PropertyName = "isDirector")]
        public bool? IsDirector { get; set; }

        /// <summary>
        /// Whether the transactor is an officer of the company
        /// </summary>
        [JsonProperty(PropertyName = "isOfficer")]
        public bool? IsOfficer { get; set; }

        /// <summary>
        /// Whether the transactor is a 10% owner of the company
        /// </summary>
        [JsonProperty(PropertyName = "isTenPercentOwner")]
        public bool? IsTenPercentOwner { get; set; }

        /// <summary>
        /// Whether the transactor is not a director, officer, or 10% owner
        /// </summary>
        [JsonProperty(PropertyName = "isOther")]
        public bool? IsOther { get; set; }

        /// <summary>
        /// The time the data point ends at and becomes available to the algorithm
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
                    $"{config.Symbol.Value.ToLowerInvariant()}.csv"
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

            var uploadedDate = Parse.DateTimeExact(csv[0], "yyyyMMdd");

            return new QuiverInsiderTrading
            {
                Time = uploadedDate.AddDays(-1),
                Symbol = config.Symbol,
                FileDate = (csv[1].IfNotNullOrEmpty<DateTime?>(s => Parse.DateTimeExact(s, "yyyyMMddHHmmss")) ?? uploadedDate).AddDays(-1),
                Date = csv[2].IfNotNullOrEmpty<DateTime?>(s => Parse.DateTimeExact(s, "yyyyMMdd")) ?? uploadedDate.AddDays(-1),
                TransactionCode = QuiverQuantCsvExtensions.ToTransactionCode(csv[3]),
                PricePerShare = csv[4].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)),
                Shares = csv[5].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)),
                SharesOwnedFollowing = csv[6].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)),
                AcquiredDisposedCode = QuiverQuantCsvExtensions.ToAcquiredDisposedCode(csv[7]),
                DirectOrIndirectOwnership = QuiverQuantCsvExtensions.ToOwnershipType(csv[8]),
                Name = csv[9],
                OfficerTitle = csv[10],
                IsDirector = QuiverQuantCsvExtensions.ToNullableBool(csv[11]),
                IsOfficer = QuiverQuantCsvExtensions.ToNullableBool(csv[12]),
                IsTenPercentOwner = QuiverQuantCsvExtensions.ToNullableBool(csv[13]),
                IsOther = QuiverQuantCsvExtensions.ToNullableBool(csv[14]),
            };
        }

        /// <summary>
        /// Converts the instance to string
        /// </summary>
        public override string ToString()
        {
            if (Data.Count > 0)
            {
                // we are the wrapper instance
                return $"{Symbol} - Data Points {Data.Count}";
            }
            return $"{Symbol} ({Name}, {OfficerTitle}) - {TransactionCode}/{AcquiredDisposedCode} - " +
                   $"{Shares} @ {PricePerShare} - SharesOwnedFollowing: {SharesOwnedFollowing} - " +
                   $"Ownership: {DirectOrIndirectOwnership} - Date: {Date} - Filed: {FileDate}";
        }

        /// <summary>
        /// Indicates whether the data source is tied to an underlying symbol and requires that corporate events be applied to it as well, such as renames and delistings
        /// </summary>
        /// <returns>false</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Clone implementation
        /// </summary>
        public override BaseData Clone()
        {
            return new QuiverInsiderTrading()
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
        /// Indicates whether the data is sparse.
        /// If true, we disable logging for missing files
        /// </summary>
        /// <returns>true</returns>
        public override bool IsSparseData()
        {
            return true;
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
