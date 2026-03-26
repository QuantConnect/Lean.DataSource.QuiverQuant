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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.DataProcessing
{
    /// <summary>
    /// Base class for Quiver data downloaders providing shared HTTP, rate-limiting, and disposal logic.
    /// </summary>
    public abstract class QuiverDataDownloader : IDisposable
    {
        public const string VendorName = "quiver";

        protected readonly string _clientKey;
        protected readonly int _maxRetries = 5;
        protected readonly bool _canCreateUniverseFiles;
        protected readonly RateGate _indexGate;

        protected readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        /// <summary>
        /// Creates a new instance of <see cref="QuiverDataDownloader"/>
        /// </summary>
        /// <param name="rateLimit">Maximum number of requests per rate period</param>
        /// <param name="ratePeriod">Time window for the rate limit</param>
        /// <param name="apiKey">Optional API key override; defaults to config value</param>
        protected QuiverDataDownloader(int rateLimit, TimeSpan ratePeriod, string apiKey = null)
        {
            _clientKey = apiKey ?? Config.Get("quiver-auth-token");
            _indexGate = new RateGate(rateLimit, ratePeriod);
            _canCreateUniverseFiles = Directory.Exists(Path.Combine(Globals.DataFolder, "equity", "usa", "map_files"));
        }

        /// <summary>
        /// Sends a GET request to the Quiver API with retries and rate limiting
        /// </summary>
        /// <param name="url">Relative URL path to request</param>
        /// <returns>Response content as string</returns>
        /// <exception cref="Exception">Thrown when all retries are exhausted</exception>
        protected async Task<string> HttpRequester(string url)
        {
            for (var retries = 1; retries <= _maxRetries; retries++)
            {
                try
                {
                    using var client = new HttpClient();
                    client.BaseAddress = new Uri("https://api.quiverquant.com/beta/");
                    client.DefaultRequestHeaders.Clear();

                    // You must supply your API key in the HTTP header,
                    // otherwise you will receive a 403 Forbidden response
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _clientKey);

                    // Responses are in JSON: you need to specify the HTTP header Accept: application/json
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Makes sure we don't overrun Quiver rate limits accidentally
                    _indexGate.WaitToProceed();

                    var response = await client.GetAsync(Uri.EscapeUriString(url));
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Log.Error($"{GetType().Name}.HttpRequester(): Files not found at url: {Uri.EscapeUriString(url)}");
                        response.DisposeSafely();
                        return string.Empty;
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var finalRequestUri = response.RequestMessage.RequestUri;
                        response = client.GetAsync(finalRequestUri).Result;
                    }

                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadAsStringAsync();
                    response.DisposeSafely();

                    return result;
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{GetType().Name}.HttpRequester(): Error at HttpRequester. (retry {retries}/{_maxRetries})");
                    Thread.Sleep(1000);
                }
            }

            throw new Exception($"Request failed with no more retries remaining (retry {_maxRetries}/{_maxRetries})");
        }

        /// <summary>
        /// Disposes of unmanaged resources
        /// </summary>
        public virtual void Dispose()
        {
            _indexGate?.Dispose();
        }
    }
}
