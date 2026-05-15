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
    /// QuiverInsiderTradingDataDownloader implementation. https://www.quiverquant.com/
    /// </summary>
    public class QuiverInsiderTradingDataDownloader : QuiverDataDownloader
    {
        public const string VendorDataName = "insidertrading";

        private readonly string _destinationFolder;
        private readonly string _universeFolder;
        private readonly string _processedDataDirectory;
        private readonly Dictionary<string, HashSet<string>> _insiderTradingByTicker = [];

        private static readonly List<char> _defunctDelimiters = new()
        {
            '-',
            '_',
            '+',
            '|',
            '='
        };

        /// <summary>
        /// Creates a new instance of <see cref="QuiverInsiderTrading"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        /// <param name="processedDataDirectory">The folder where the data will be read from</param>
        /// <param name="apiKey">The QuiverQuant API key</param>
        public QuiverInsiderTradingDataDownloader(string destinationFolder, string processedDataDirectory, string apiKey = null)
            : base(2, TimeSpan.FromSeconds(1), apiKey)
        {
            _destinationFolder = Path.Combine(destinationFolder, VendorName, VendorDataName);
            _universeFolder = Path.Combine(_destinationFolder, "universe");
            _processedDataDirectory = Path.Combine(processedDataDirectory, VendorName, VendorDataName);

            Directory.CreateDirectory(_universeFolder);
        }

        public QuiverInsiderTradingDataDownloader()
            : base(2, TimeSpan.FromSeconds(1))
        {
        }

        /// <summary>
        /// Fetches a single day of insider trading data and accumulates it per-ticker in memory.
        /// </summary>
        /// <param name="processDate">The date of data to be fetched</param>
        /// <returns>True if the day was fetched and parsed successfully</returns>
        public bool Run(DateTime processDate)
        {
            var stopwatch = Stopwatch.StartNew();
            Log.Trace($"QuiverInsiderTradingDataDownloader.Run(): Start downloading QuiverQuant Insider Trading data for {processDate:yyyy-MM-dd}");

            var today = DateTime.UtcNow.Date;
            try
            {
                if (processDate > today || processDate == DateTime.MinValue)
                {
                    Log.Trace($"Encountered data from invalid date: {processDate:yyyy-MM-dd} - Skipping");
                    return false;
                }

                var quiverInsiderTradingData = HttpRequester($"live/insiders?date={processDate:yyyyMMdd}").SynchronouslyAwaitTaskResult();
                if (string.IsNullOrWhiteSpace(quiverInsiderTradingData))
                {
                    return false;
                }

                var insiderTradingByDate = JsonConvert.DeserializeObject<List<RawInsiderTrading>>(quiverInsiderTradingData, _jsonSerializerSettings);

                foreach (var insiderTrade in insiderTradingByDate)
                {
                    var quiverTicker = insiderTrade.Ticker;
                    if (quiverTicker == null) continue;

                    if (insiderTrade.Uploaded == null)
                    {
                        Log.Trace($"QuiverInsiderTradingDataDownloader.Run(): Skipping row with null Uploaded for ticker {quiverTicker} on {processDate:yyyyMMdd}");
                        continue;
                    }

                    if (!TryNormalizeDefunctTicker(quiverTicker, out var tickerList))
                    {
                        Log.Error($"QuiverInsiderTradingDataDownloader.Run(): Defunct ticker {quiverTicker} is unable to be parsed. Continuing...");
                        continue;
                    }

                    var uploadedDate = insiderTrade.Uploaded.Value.Date;
                    // Omit fileDate when its calendar day matches uploaded. Reader falls back to uploadedDate,
                    // preserving the day but dropping intraday precision (acceptable trade-off for storage).
                    var fileDate = insiderTrade.FileDate == default || insiderTrade.FileDate.Date == uploadedDate
                        ? string.Empty
                        : insiderTrade.FileDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    var transactionDate = insiderTrade.Date == default
                        ? string.Empty
                        : insiderTrade.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    var name = SanitizeCsv(insiderTrade.Name);
                    var officerTitle = SanitizeCsv(insiderTrade.OfficerTitle);
                    var transactionCode = insiderTrade.TransactionCode.ToCsv();
                    var ownership = insiderTrade.DirectOrIndirectOwnership.ToCsv();
                    var acquiredDisposed = insiderTrade.AcquiredDisposedCode.ToCsv();

                    var line = $"{uploadedDate:yyyyMMdd},{fileDate},{transactionDate}," +
                               $"{transactionCode},{insiderTrade.PricePerShare},{insiderTrade.Shares},{insiderTrade.SharesOwnedFollowing}," +
                               $"{acquiredDisposed},{ownership},{name},{officerTitle}," +
                               $"{insiderTrade.IsDirector.ToCsv()},{insiderTrade.IsOfficer.ToCsv()},{insiderTrade.IsTenPercentOwner.ToCsv()},{insiderTrade.IsOther.ToCsv()}";

                    foreach (var rawTicker in tickerList)
                    {
                        var ticker = Regex.Replace(rawTicker, @"[^A-Z0-9.]", string.Empty).Trim('.');
                        if (!Regex.IsMatch(ticker, @"^[A-Z0-9][A-Z0-9.]*[A-Z0-9]$") && !Regex.IsMatch(ticker, @"^[A-Z0-9]$"))
                        {
                            Log.Trace($"QuiverInsiderTradingDataDownloader.Run(): Skipping invalid ticker '{rawTicker}' on {processDate:yyyyMMdd}");
                            continue;
                        }
                        if (!_insiderTradingByTicker.TryGetValue(ticker, out var lines))
                        {
                            _insiderTradingByTicker[ticker] = lines = [];
                        }
                        lines.Add(line);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"QuiverInsiderTradingDataDownloader.Run(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }

        /// <summary>
        /// Writes every accumulated per-ticker batch to disk, merging with any pre-existing file.
        /// </summary>
        /// <returns>True on success</returns>
        public bool Flush()
        {
            if (_insiderTradingByTicker.Count == 0)
            {
                Log.Error($"QuiverInsiderTradingDataDownloader.Flush(): No data accumulated; treating run as a failure (likely unable to reach QuiverQuant).");
                return false;
            }

            var failed = 0;
            foreach (var kvp in _insiderTradingByTicker)
            {
                try
                {
                    SaveContentToFile(kvp.Key, kvp.Value);
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error(e, $"QuiverInsiderTradingDataDownloader.Flush(): Failed to write data for ticker '{kvp.Key}'");
                }
            }
            return failed == 0;
        }

        /// <summary>
        /// Regenerates the universe files keyed by upload date by reading every per-ticker file.
        /// </summary>
        /// <returns>True if the universe files were regenerated successfully</returns>
        public bool ProcessUniverse()
        {
            if (!_canCreateUniverseFiles)
            {
                Log.Trace("QuiverInsiderTradingDataDownloader.ProcessUniverse(): Map files not available, skipping universe generation");
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            Log.Trace("QuiverInsiderTradingDataDownloader.ProcessUniverse(): Start regenerating universe files by upload date");

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

                        SecurityIdentifier sid;
                        try
                        {
                            sid = SecurityIdentifier.GenerateEquity(ticker, Market.USA, true, mapFileProvider, uploadDate);
                        }
                        catch (Exception)
                        {
                            Log.Error($"QuiverInsiderTradingDataDownloader.ProcessUniverse(): Invalid ticker {ticker} on {uploadDate:yyyyMMdd}. Skipping line.");
                            continue;
                        }

                        if (sid.Date == SecurityIdentifier.DefaultDate || sid.ToString().Contains(" 2T")) continue;

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

            Log.Trace($"QuiverInsiderTradingDataDownloader.ProcessUniverse(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }

        /// <summary>
        /// Saves per-ticker contents to disk, merging with any pre-existing file.
        /// </summary>
        /// <param name="name">File name (ticker)</param>
        /// <param name="contents">Contents to write</param>
        private void SaveContentToFile(string name, IEnumerable<string> contents)
        {
            name = name.ToLowerInvariant();
            var finalPath = Path.Combine(_destinationFolder, $"{name}.csv");
            var existingPath = Path.Combine(_processedDataDirectory, $"{name}.csv");

            HashSet<string> lines = [.. contents];
            foreach (var path in new[] { existingPath, finalPath })
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

        private static string SanitizeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace(",", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        }

        /// <summary>
        /// Tries to normalize a potentially defunct ticker into a normal ticker.
        /// </summary>
        /// <param name="rawTicker">Ticker as received from InsiderTrading</param>
        /// <param name="tickerList">an array the non-defunct ticker of the same company</param>
        /// <returns>true for success, false for failure</returns>
        protected static bool TryNormalizeDefunctTicker(string rawTicker, out string[] tickerList)
        {
            var ticker = rawTicker.Split(':').Last().Replace("\"", string.Empty).ToUpperInvariant().Trim();
            foreach (var delimChar in _defunctDelimiters)
            {
                var length = ticker.IndexOf(delimChar);

                // Continue until we exhaust all delimiters
                if (length == -1)
                {
                    continue;
                }

                tickerList = ticker.Substring(0, length).Trim().Split(' ');
                return true;
            }

            tickerList = ticker.Split(' ');
            return true;
        }

        private class RawInsiderTrading : QuiverInsiderTrading
        {
            [JsonProperty(PropertyName = "Ticker")]
            public string Ticker { get; set; } = null!;

            [JsonProperty(PropertyName = "uploaded")]
            public DateTime? Uploaded { get; set; }
        }

    }
}
