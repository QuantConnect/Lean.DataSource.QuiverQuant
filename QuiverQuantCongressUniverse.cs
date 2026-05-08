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
using QuantConnect.Orders;
using static QuantConnect.StringExtensions;

namespace QuantConnect.DataSource
{
    /// <summary>
    /// Universe Selection helper class for QuiverQuant Congress dataset
    /// </summary>
    public class QuiverQuantCongressUniverse : BaseDataCollection
    {
        /// <summary>
        /// The date the transaction was recorded by QuiverQuant. Value will always exist.
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// The date the recorded transaction was updated by QuiverQuant. Alias for EndTime.
        /// </summary>
        public DateTime UpdatedAt => EndTime;

        /// <summary>
        /// The date the transaction was reported. Value will always exist.
        /// </summary>
        public DateTime? ReportDate { get; set; }

        /// <summary>
        /// The date the transaction took place
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// The Representative making the transaction
        /// </summary>
        public string Representative { get; set; }

        /// <summary>
        /// The type of transaction
        /// </summary>
        public OrderDirection Transaction { get; set; }

        /// <summary>
        /// The amount of the transaction (in USD). The Representative can report a range (see <see cref="MaximumAmount"/>).
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// The maximum amount of the transaction (in USD). The Representative can report a range (see <see cref="Amount"/>).
        /// </summary>
        public decimal? MaximumAmount { get; set; }

        /// <summary>
        /// The Chamber of Congress that the trader belongs to
        /// </summary>
        public Congress House { get; set; }

        /// <summary>
        /// The political party that the trader belongs to
        /// </summary>
        public Party Party { get; set; }

        /// <summary>
        /// The district that the trader belongs to (null or empty for Senators)
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// The state that the trader belongs to
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Time the data became available
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
                    "congresstrading",
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
            var amount = csv[7].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
            var maximumAmount = csv[8].IfNotNullOrEmpty<decimal?>(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));

            return new QuiverQuantCongressUniverse
            {
                RecordDate = Parse.DateTimeExact(csv[2], "yyyyMMdd"),
                ReportDate = Parse.DateTimeExact(csv[3], "yyyyMMdd"),
                TransactionDate = Parse.DateTimeExact(csv[4], "yyyyMMdd"),
                Representative = csv[5].Replace(";", ","),
                Transaction = (OrderDirection)Enum.Parse(typeof(OrderDirection), csv[6], true),
                Amount = amount,
                MaximumAmount = maximumAmount,
                House = (Congress)Enum.Parse(typeof(Congress), csv[9], true),
                Party = (Party)Enum.Parse(typeof(Party), csv[10], true),
                District = csv[11],
                State = csv[12],
                Symbol = new Symbol(SecurityIdentifier.Parse(csv[0]), csv[1]),
                Value = amount ?? 0,
                Time = date
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
        /// Converts the instance to string
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Symbol}({EndTime:yyyyMMdd}) :: ") +
                   Invariant($"Representative: {Representative} ") +
                   Invariant($"House: {House} ") +
                   Invariant($"Transaction: {Transaction} ") +
                   Invariant($"Amount: {Amount}");
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

        /// <summary>
        /// Clones the data
        /// </summary>
        /// <returns>A clone of the object</returns>
        public override BaseData Clone()
        {
            return new QuiverQuantCongressUniverse
            {
                Symbol = Symbol,
                Time = Time,
                Data = Data,
                Value = Value,

                RecordDate = RecordDate,
                ReportDate = ReportDate,
                TransactionDate = TransactionDate,
                Representative = Representative,
                Transaction = Transaction,
                Amount = Amount,
                MaximumAmount = MaximumAmount,
                House = House,
                Party = Party,
                District = District,
                State = State,
            };
        }
    }
}
