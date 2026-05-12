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
using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.DataSource
{
    /// <summary>
    ///  Personal stock advice by CNBC
    /// </summary>
    public class QuiverCNBC : BaseData
    {
        /// <summary>
        /// Contract description
        /// </summary>
        [JsonProperty(PropertyName = "Notes")]
        public string Notes { get; set; }
        
        /// <summary>
        /// Direction of trade
        /// </summary>
        [JsonProperty(PropertyName = "Direction")]
        [JsonConverter(typeof(TransactionDirectionJsonConverter))]
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Individual Name
        /// </summary>
        [JsonProperty(PropertyName = "Traders")]
        public string Traders { get; set; }

        /// <summary>
        /// Date the trader issued the stock advice on CNBC
        /// </summary>
        public DateTime AdviceDate { get; set; }

        /// <summary>
        /// Time the data became available
        /// </summary>
        public override DateTime EndTime => Time.AddDays(1);

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

            return new QuiverCNBC
            {
                Symbol = config.Symbol,
                Time = uploadedDate.AddDays(-1),
                AdviceDate = (csv[1].IfNotNullOrEmpty<DateTime?>(s => Parse.DateTimeExact(s, "yyyyMMdd")) ?? uploadedDate).AddDays(-1),
                Direction = QuiverQuant.QuiverQuantCsvExtensions.ToOrderDirection(csv[2]),
                Traders = csv[3],
                Notes = csv.Length > 4 ? csv[4] : string.Empty,
            };
        }

        /// <summary>
        /// Clones the data
        /// </summary>
        /// <returns>A clone of the object</returns>
        public override BaseData Clone()
        {
            return new QuiverCNBC
            {
                Symbol = Symbol,
                Time = Time,
                AdviceDate = AdviceDate,
                Notes = Notes,
                Direction = Direction,
                Traders = Traders,
            };
        }

        /// <summary>
        /// Converts the instance to string
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol} - {Traders} - {Direction}";
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
            return DateTimeZone.Utc;
        }
    }
}
