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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using QuantConnect.Data.Auxiliary;
using QuantConnect.DataSource;
using QuantConnect.DataSource.QuiverQuant;
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
        private readonly Dictionary<string, HashSet<string>> _cnbcByTicker = [];

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

                foreach (var cnbc in cnbcByDate)
                {
                    var ticker = cnbc.Ticker;
                    if (ticker == null)
                    {
                        Log.Error($"QuiverCNBCDataDownloader.Run(): Null value for Ticker on {processDate:yyyyMMdd}");
                        continue;
                    }

                    ticker = ticker.Split(':').Last().Replace("\"", string.Empty).ToUpperInvariant().Trim();
                    // Strip characters not allowed in tickers (only letters, digits, and mid-string dots are valid)
                    ticker = Regex.Replace(ticker, @"[^A-Z0-9.]", string.Empty);
                    ticker = ticker.Trim('.');

                    // Validate: must be non-empty, only letters/digits/dots, dot not at start or end
                    if (!Regex.IsMatch(ticker, @"^[A-Z0-9][A-Z0-9.]*[A-Z0-9]$") && !Regex.IsMatch(ticker, @"^[A-Z0-9]$"))
                    {
                        Log.Trace($"QuiverCNBCDataDownloader.Run(): Skipping invalid ticker '{ticker}' on {processDate:yyyyMMdd}");
                        continue;
                    }

                    var note = SanitizeCsv(cnbc.Notes);
                    var traders = SanitizeCsv(cnbc.Traders);
                    var curRow = $"{cnbc.Direction.ToCsv()},{traders},{note}";
                    var uploadDate = cnbc.UploadDate?.Date ?? processDate;
                    // csv[0] is always the uploadDate. csv[1] (adviceDate) is omitted when it equals uploadDate;
                    // Reader falls back to uploadedDate in that case.
                    var adviceDateCol = uploadDate == processDate
                        ? string.Empty
                        : $"{processDate:yyyyMMdd}";
                    var line = $"{uploadDate:yyyyMMdd},{adviceDateCol},{curRow}";

                    if (!_cnbcByTicker.TryGetValue(ticker, out var lines))
                    {
                        _cnbcByTicker[ticker] = lines = [];
                    }
                    lines.Add(line);
                }
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
        /// Writes every accumulated per-ticker batch to disk, merging with any pre-existing file.
        /// </summary>
        /// <returns>True on success</returns>
        public bool Flush()
        {
            try
            {
                foreach (var kvp in _cnbcByTicker)
                {
                    SaveContentToFile(_destinationFolder, kvp.Key, kvp.Value);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Regenerates the universe files keyed by upload date by reading every per-ticker file.
        /// </summary>
        /// <returns>True if the universe files were regenerated successfully</returns>
        public bool ProcessUniverse()
        {
            if (!_canCreateUniverseFiles)
            {
                Log.Trace($"QuiverCNBCDataDownloader.ProcessUniverse(): Map files not available, skipping universe generation");
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            Log.Trace($"QuiverCNBCDataDownloader.ProcessUniverse(): Start regenerating universe files by upload date");

            try
            {
                var mapFileProvider = new LocalZipMapFileProvider();
                mapFileProvider.Initialize(new DefaultDataProvider());

                Dictionary<DateTime, HashSet<string>> dataByUploadDate = [];

                void processFile(string filePath)
                {
                    var ticker = Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();
                    foreach (var line in File.ReadAllLines(filePath))
                    {
                        var firstComma = line.IndexOf(',');
                        if (firstComma <= 0) continue;

                        var uploadDateStr = line[..firstComma];
                        if (!DateTime.TryParseExact(uploadDateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var uploadDate))
                        {
                            continue;
                        }
                        var rest = line[(firstComma + 1)..];

                        if (!dataByUploadDate.TryGetValue(uploadDate, out var data))
                        {
                            dataByUploadDate[uploadDate] = data = [];
                        }

                        var sid = SecurityIdentifier.GenerateEquity(ticker, Market.USA, true, mapFileProvider, uploadDate);
                        data.Add($"{sid},{ticker},{rest}");
                    }
                }

                if (Directory.Exists(_processedDataDirectory))
                {
                    Directory.EnumerateFiles(_processedDataDirectory, "*.csv").DoForEach(processFile);
                }
                Directory.EnumerateFiles(_destinationFolder, "*.csv").DoForEach(processFile);

                dataByUploadDate.DoForEach(kvp =>
                {
                    var filePath = Path.Combine(_universeFolder, $"{kvp.Key:yyyyMMdd}.csv");
                    File.WriteAllLines(filePath, kvp.Value.OrderBy(x => x));
                });
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"QuiverCNBCDataDownloader.ProcessUniverse(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }

        /// <summary>
        /// Saves per-ticker contents to disk, merging with any pre-existing file
        /// </summary>
        /// <param name="destinationFolder">Final destination of the data</param>
        /// <param name="name">file name</param>
        /// <param name="contents">Contents to write</param>
        private static string SanitizeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace(",", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        }

        private void SaveContentToFile(string destinationFolder, string name, IEnumerable<string> contents)
        {
            name = name.ToLowerInvariant();
            var finalPath = Path.Combine(destinationFolder, $"{name}.csv");
            var filePath = Path.Combine(_processedDataDirectory, $"{name}.csv");

            HashSet<string> lines = [.. contents];
            foreach (var path in new[] { filePath, finalPath })
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        lines.Add(line);
                    }
                }
            }

            var finalLines = lines
                .OrderBy(x => DateTime.ParseExact(x.Split(',')[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
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

            /// <summary>
            /// The date this data was uploaded to QuiverQuant's database
            /// </summary>
            [JsonProperty(PropertyName = "upload_time")]
            [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
            public DateTime? UploadDate { get; set; }
        }

    }
}
