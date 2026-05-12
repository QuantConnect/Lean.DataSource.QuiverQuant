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

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.DataSource.QuiverQuant
{
    /// <summary>
    /// SEC Form 4 indicator of whether the transaction was an acquisition or a disposal
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AcquiredDisposedCode
    {
        /// <summary>
        /// Default value used when no acquired/disposed flag is provided or the value is unrecognized
        /// </summary>
        [EnumMember(Value = "")]
        Unknown,

        /// <summary>
        /// A - Share acquisition
        /// </summary>
        [EnumMember(Value = "A")]
        Acquired,

        /// <summary>
        /// D - Share disposal
        /// </summary>
        [EnumMember(Value = "D")]
        Disposed,
    }
}
