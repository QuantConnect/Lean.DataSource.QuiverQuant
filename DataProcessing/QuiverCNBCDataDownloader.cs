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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Data.Auxiliary;
using QuantConnect.DataSource;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.DataProcessing
{
    /// <summary>
    /// QuiverCNBCDataDownloader implementation.
    /// </summary>
    public class QuiverCNBCDataDownloader : QuiverDataDownloader
    {
        public const string VendorDataName = "cnbc";

        private readonly string _destinationFolder;
        private readonly string _universeFolder;
        private readonly string _processedDataDirectory;

        /// <summary>
        /// Creates a new instance of <see cref="QuiverCNBC"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        /// <param name="processedDataDirectory">The folder where the data will be read from</param>
        /// <param name="apiKey">The Vendor API key</param>
        public QuiverCNBCDataDownloader(string destinationFolder, string processedDataDirectory, string apiKey = null)
            : base(100, TimeSpan.FromSeconds(60), apiKey)
        {
            _destinationFolder = Path.Combine(destinationFolder, VendorName, VendorDataName);
            _universeFolder = Path.Combine(_destinationFolder, "universe");
            _processedDataDirectory = Path.Combine(processedDataDirectory, VendorName, VendorDataName);

            Directory.CreateDirectory(_universeFolder);
        }

        /// <summary>
        /// Runs the instance of the object with a given date.
        /// </summary>
        /// <param name="processDate">The date of data to be fetched and processed</param>
        /// <returns>True if process all downloads successfully</returns>
        public bool Run(DateTime processDate)
        {
            var stopwatch = Stopwatch.StartNew();
            Log.Trace($"QuiverCNBCDataDownloader.Run(): Start downloading/processing QuiverQuant CNBC data");

            var today = DateTime.UtcNow.Date;
            try
            {
                if (processDate >= today || processDate == DateTime.MinValue)
                {
                    Log.Trace($"Encountered data from invalid date: {processDate:yyyy-MM-dd} - Skipping");
                    return false;
                }

                var quiverCnbcData = HttpRequester($"live/cnbc?date={processDate:yyyyMMdd}").SynchronouslyAwaitTaskResult();
                if (string.IsNullOrWhiteSpace(quiverCnbcData))
                {
                    // We've already logged inside HttpRequester
                    return false;
                }

                var cnbcByDate = JsonConvert.DeserializeObject<List<RawCNBC>>(quiverCnbcData, _jsonSerializerSettings);

                var cnbcByTicker = new Dictionary<string, List<string>>();
                var universeCsvContents = new List<string>();

                var mapFileProvider = new LocalZipMapFileProvider();
                mapFileProvider.Initialize(new DefaultDataProvider());

                foreach (var cnbc in cnbcByDate)
                {
                    var ticker = cnbc.Ticker;
                    if (ticker == null) 
                    {
                        Log.Error($"QuiverCNBCDataDownloader.Run(): Null value for Ticker on {processDate:yyyyMMdd}");
                        continue;
                    }

                    ticker = ticker.Split(':').Last().Replace("\"", string.Empty).ToUpperInvariant().Trim();

                    if (!cnbcByTicker.TryGetValue(ticker, out var _))
                    {
                        cnbcByTicker.Add(ticker, new List<string>());
                    }

                    var note = cnbc.Notes != null ? cnbc.Notes.Replace(Environment.NewLine, string.Empty).Trim() : null;
                    var curRow = $"{note},{cnbc.Direction},{cnbc.Traders.Trim()}";
                    cnbcByTicker[ticker].Add($"{processDate:yyyyMMdd},{curRow}");

                    var sid = SecurityIdentifier.GenerateEquity(ticker, Market.USA, true, mapFileProvider, processDate);
                    universeCsvContents.Add($"{sid},{ticker},{curRow}");
                }

                if (!_canCreateUniverseFiles)
                {
                    return false;
                }
                else if (universeCsvContents.Any())
                {
                    SaveContentToFile(_universeFolder, $"{processDate:yyyyMMdd}", universeCsvContents);
                }

                cnbcByTicker.DoForEach(kvp => SaveContentToFile(_destinationFolder, kvp.Key, kvp.Value));
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"QuiverCNBCDataDownloader.Run(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }

        /// <summary>
        /// Saves contents to disk, deleting existing zip files
        /// </summary>
        /// <param name="destinationFolder">Final destination of the data</param>
        /// <param name="name">file name</param>
        /// <param name="contents">Contents to write</param>
        private void SaveContentToFile(string destinationFolder, string name, IEnumerable<string> contents)
        {
            name = name.ToLowerInvariant();
            var finalPath = Path.Combine(destinationFolder, $"{name}.csv");
            string filePath;

            if (destinationFolder.Contains("universe"))
            {
                filePath = Path.Combine(_processedDataDirectory, "universe", $"{name}.csv");
            }
            else
            {
                filePath = Path.Combine(_processedDataDirectory, $"{name}.csv");
            }

            var finalFileExists = File.Exists(filePath);

            var lines = new HashSet<string>(contents);
            if (finalFileExists)
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    lines.Add(line);
                }
            }

            var finalLines = destinationFolder.Contains("universe") ? 
                lines.OrderBy(x => x.Split(',').First()).ToList() :
                lines
                .OrderBy(x => DateTime.ParseExact(x.Split(',').First(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                .ToList();

            File.WriteAllLines(finalPath, finalLines);
        }

        private class RawCNBC: QuiverCNBC
        {
            /// <summary>
            /// Date that the CNBC data was received
            /// </summary>
            [JsonProperty(PropertyName = "Date")]
            [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
            public DateTime Date { get; set; }

            /// <summary>
            /// The ticker/symbol for the company
            /// </summary>
            [JsonProperty(PropertyName = "Ticker")]
            public string Ticker { get; set; }
        }

    }
}
