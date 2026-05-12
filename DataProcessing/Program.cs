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
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.DataProcessing
{
    /// <summary>
    /// Entrypoint for the data downloader/converter
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entrypoint of the program
        /// </summary>
        /// <returns>Exit code. 0 equals successful, and any other value indicates the downloader/converter failed.</returns>
        public static void Main(string[] args)
        {
            var dataset = args.Length > 0 ? args[0].ToLowerInvariant() : "cnbc";
            var destinationDirectory = Path.Combine(
                Config.Get("temp-output-directory", "/temp-output-directory"),
                "alternative");
            var processedDataDirectory = Path.Combine(
                Config.Get("processed-data-directory", Globals.DataFolder),
                "alternative");
            var processingDateValue = Config.Get("processing-date", Environment.GetEnvironmentVariable("QC_DATAFLEET_DEPLOYMENT_DATE"))
                ?? DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");
            var processingDate = Parse.DateTimeExact(processingDateValue, "yyyyMMdd");
            var processingDateLookback = Config.GetInt("processing-date-lookback", 0);
            var processingStartDate = processingDate.AddDays(-processingDateLookback);

            switch (dataset.ToLowerInvariant())
            {
                case "cnbc":
                {
                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverCNBCDataDownloader.VendorDataName,
                        () => new QuiverCNBCDataDownloader(destinationDirectory, processedDataDirectory),
                        instance =>
                        {
                            for (var date = processingStartDate; date <= processingDate; date = date.AddDays(1))
                            {
                                if (!instance.Run(date))
                                {
                                    Log.Error($"QuantConnect.DataProcessing.Program.Main(): Failed to download/process " +
                                        $"{QuiverDataDownloader.VendorName} {QuiverCNBCDataDownloader.VendorDataName} data for date: {date:yyyy-MM-dd}");
                                }
                            }
                            instance.Flush();
                            instance.ProcessUniverse();
                            return true;
                        });
                    break;
                }

                case "governmentcontract":
                {
                    var datasetStartDate = new DateTime(2022, 4, 21);
                    if (processingDate < datasetStartDate)
                    {
                        Log.Error($"QuantConnect.DataProcessing.Program.Main(): Invalid processing date, must be greater than {datasetStartDate:yyyyMMdd}.");
                        Environment.Exit(1);
                    }

                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverGovernmentContractDownloader.VendorDataName,
                        () => new QuiverGovernmentContractDownloader(),
                        instance =>
                        {
                            var success = instance.Run(processingDate);
                            if (!success)
                            {
                                Log.Error($"QuantConnect.DataProcessing.Program.Main(): Failed to download/process " +
                                    $"{QuiverDataDownloader.VendorName} {QuiverGovernmentContractDownloader.VendorDataName} data for date: {processingDate:yyyy-MM-dd}");
                            }
                            instance.ProcessUniverse();
                            return true;
                        });
                    break;
                }

                case "lobbying":
                {
                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverLobbyingDataDownloader.VendorDataName,
                        () => new QuiverLobbyingDataDownloader(destinationDirectory, processedDataDirectory),
                        instance => instance.Run(processingDate));
                    break;
                }

                case "congresstrading":
                {
                    var congressDestination = Path.Combine(destinationDirectory, "quiver");
                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverCongressDataDownloader.VendorDataName,
                        () => new QuiverCongressDataDownloader(congressDestination),
                        instance => instance.Run());
                    break;
                }

                case "wallstreetbets":
                {
                    var tempOutput = Config.Get("temp-output-directory", "/temp-output-directory");
                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverWallStreetBetsDataDownloader.VendorDataName,
                        () => new QuiverWallStreetBetsDataDownloader(tempOutput),
                        instance => instance.Run());
                    break;
                }

                case "insidertrading":
                {
                    RunDownloader(
                        QuiverDataDownloader.VendorName,
                        QuiverInsiderTradingDataDownloader.VendorDataName,
                        () => new QuiverInsiderTradingDataDownloader(destinationDirectory, processedDataDirectory),
                        instance =>
                        {
                            for (var date = processingStartDate; date <= processingDate; date = date.AddDays(1))
                            {
                                if (!instance.Run(date))
                                {
                                    Log.Error($"QuantConnect.DataProcessing.Program.Main(): Failed to download/process " +
                                        $"{QuiverDataDownloader.VendorName} {QuiverInsiderTradingDataDownloader.VendorDataName} data for date: {date:yyyy-MM-dd}");
                                }
                            }
                            instance.Flush();
                            instance.ProcessUniverse();
                            return true;
                        });
                    break;
                }

                default:
                    Log.Error($"Unknown dataset '{dataset}'");
                    break;
            }

            Environment.Exit(0);
        }

        private static void RunDownloader<T>(string vendorName, string vendorDataName, Func<T> factory, Func<T, bool> run) where T : class, IDisposable
        {
            T instance = null;
            try
            {
                instance = factory();
            }
            catch (Exception err)
            {
                Log.Error(err, $"QuantConnect.DataProcessing.Program.Main(): The downloader/converter for {vendorName} {vendorDataName} data failed to be constructed");
                Environment.Exit(1);
            }

            try
            {
                if (!run(instance))
                {
                    Log.Error($"QuantConnect.DataProcessing.Program.Main(): Failed to download/process {vendorName} {vendorDataName} data");
                    Environment.Exit(1);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, $"QuantConnect.DataProcessing.Program.Main(): The downloader/converter for {vendorName} {vendorDataName} data exited unexpectedly");
                Environment.Exit(1);
            }
            finally
            {
                instance.DisposeSafely();
            }
        }
    }
}