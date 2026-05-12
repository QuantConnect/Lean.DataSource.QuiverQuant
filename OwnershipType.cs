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
    /// SEC Form 4 direct or indirect ownership classification
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OwnershipType
    {
        /// <summary>
        /// Default value used when no ownership flag is provided or the value is unrecognized
        /// </summary>
        [EnumMember(Value = "")]
        Unknown,

        /// <summary>
        /// D - Direct ownership of the security by the reporting person
        /// </summary>
        [EnumMember(Value = "D")]
        Direct,

        /// <summary>
        /// I - Indirect ownership of the security (e.g., through a trust or family member)
        /// </summary>
        [EnumMember(Value = "I")]
        Indirect,
    }
}
